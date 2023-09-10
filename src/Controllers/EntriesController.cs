using System.Security.Cryptography;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using FileSharing.Data;
using FileSharing.Models;
using System.Text;

namespace FileSharing.Controllers
{
    public class EntriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Random _random;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        public EntriesController(ApplicationDbContext context, IConfiguration configuration, ILogger<EntriesController> logger)
        {
            _context = context;
            _random = new Random();
            _configuration = configuration;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Create));
        }

        public async Task<IActionResult> Download(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction(nameof(Index));
            }

            var entryDbModel = await _context.Entry.FirstOrDefaultAsync(x => x.guid == id.ToLower());

            if (string.IsNullOrWhiteSpace(entryDbModel?.guid))
            {
                return RedirectToAction(nameof(Index));
            }


            if (entryDbModel.maxNumOfDownloads == 0)
            {
                System.IO.File.Delete("uploads/"+entryDbModel.guid+".niei");
                _context.Remove(entryDbModel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            else
            {
                entryDbModel.maxNumOfDownloads--;
                _context.Update(entryDbModel);
                await _context.SaveChangesAsync();
            }

            var path = $"uploads/{entryDbModel.guid}.niei";

            // Check if the file exists
            if (!System.IO.File.Exists(path))
            {
                return NotFound();
            }

            var aesKey = entryDbModel.aesKey;
            var iv = entryDbModel.iv;

            // Read the encrypted content from the file
            byte[] encryptedContent = System.IO.File.ReadAllBytes(path);

            // Decrypt the content
            byte[] decryptedContent;
            using (AesCryptoServiceProvider aesDecryptor = new())
            {
                aesDecryptor.Key = aesKey;
                aesDecryptor.IV = iv;
                using (ICryptoTransform decryptor = aesDecryptor.CreateDecryptor())
                using (MemoryStream decryptedStream = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(decryptedStream, decryptor, CryptoStreamMode.Write))
                {
                    cs.Write(encryptedContent, 0, encryptedContent.Length);
                    cs.FlushFinalBlock();

                    decryptedContent = decryptedStream.ToArray();
                }
            }

            var ipAddr = GetRequestIp(Request);

            _logger.LogInformation($"{ipAddr} downloaded {id}");

            // Set the content type based on the file extension
            string contentType = "application/octet-stream";

            // Return the decrypted file for download
            return File(decryptedContent, contentType, entryDbModel.originalFileName);
        }

        public async Task<IActionResult> Details(string? id)
        {
            var entryDbModel = new Entry { };

            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction(nameof(Index));
            }

            if (id.Length == 36)
            {
                entryDbModel = await _context.Entry.FirstOrDefaultAsync(x => x.guid == id.ToLower());
            }

            else
            {
                entryDbModel = await _context.Entry.FirstOrDefaultAsync(x => x.customId == id);
            }

            if (string.IsNullOrWhiteSpace(entryDbModel?.guid))
            {
                return RedirectToAction(nameof(Index));
            }

            if (entryDbModel.expiresAt < DateTime.UtcNow)
            {
                System.IO.File.Delete("uploads/" + entryDbModel.guid + ".niei");
                _context.Remove(entryDbModel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var ipAddr = GetRequestIp(Request);

            _logger.LogInformation($"{ipAddr} viewed {entryDbModel.guid}");

            return View(new EntryViewModel(entryDbModel));
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EntryViewModel entryViewModel, IFormFile uploadedFile)
        {
            if (uploadedFile == null || uploadedFile.Length <= 0)
            {
                ModelState.AddModelError("uploadedFile", "Please select a file to upload.");
                return View(entryViewModel);
            }

            var enteredPasswordHash = SHA512(entryViewModel.broToken ?? string.Empty);
            var realPasswordHash = _configuration.GetValue<string>("Hashbrown");

            if (string.IsNullOrWhiteSpace(entryViewModel.broToken) || enteredPasswordHash != realPasswordHash)
            {
                entryViewModel.broToken = string.Empty;
                entryViewModel.validationError = "BroToken™ invalid";
                return View(entryViewModel);
            }

            var aesKey = new byte[32];
            var iv = new byte[16];

            using (Aes aesAlgorithm = Aes.Create())
            {
                aesAlgorithm.KeySize = 256;
                aesAlgorithm.GenerateKey();
                aesKey = aesAlgorithm.Key;
                iv = aesAlgorithm.IV;
            }

            var entryDbModel = new Entry {
                guid = Guid.NewGuid().ToString(),
                aesKey = aesKey,
                iv = iv,
                maxNumOfDownloads = entryViewModel.maxNumOfDownloads >= 100 ? 100 : entryViewModel.maxNumOfDownloads,
                originalFileName = uploadedFile.FileName
            };

            entryDbModel.expiresAt = DateTime.UtcNow.AddHours(entryViewModel.expiresIn >= 720.00 ? 720.00 : entryViewModel.expiresIn);

            if (entryViewModel.customId != null)
            {
                entryViewModel.customId = entryViewModel.customId.ToLower();

                var pattern = @"^[a-z0-9]{1,16}$";

                var regex = new Regex(pattern, RegexOptions.IgnoreCase);

                var customIdIsValid = ContainsLetterAndNotJustNumbers(entryViewModel.customId) && regex.IsMatch(entryViewModel.customId);
                var customIdAlreadyExists = await _context.Entry.AnyAsync(x => x.customId == entryViewModel.customId);
                var customIdLengthIsValid = entryViewModel.customId.Length >= 1 && entryViewModel.customId.Length <= 16;
                var customIdContainsBadStuff = entryViewModel.customId.Contains('/') || entryViewModel.customId.Contains("home") || entryViewModel.customId.Contains("entries") || entryViewModel.customId.Contains("changetheme");

                if (!customIdIsValid || customIdAlreadyExists || !customIdLengthIsValid || customIdContainsBadStuff)
                {
                    entryViewModel.validationError = "Custom ID invalid or already in use";
                    return View(entryViewModel);
                }
                entryDbModel.customId = entryViewModel.customId;
            }

            using (var memoryStream = new MemoryStream())
            {
                await uploadedFile.CopyToAsync(memoryStream);
                byte[] uploadedFileContent = memoryStream.ToArray();

                // Encrypt uploaded file content
                using (AesCryptoServiceProvider aesEncryptor = new AesCryptoServiceProvider())
                {
                    aesEncryptor.Key = aesKey;
                    aesEncryptor.IV = iv;
                    using (ICryptoTransform encryptor = aesEncryptor.CreateEncryptor())
                    using (MemoryStream encryptedStream = new MemoryStream())
                    using (CryptoStream cs = new CryptoStream(encryptedStream, encryptor, CryptoStreamMode.Write))
                    {
                        await cs.WriteAsync(uploadedFileContent, 0, uploadedFileContent.Length);
                        cs.FlushFinalBlock();

                        byte[] encryptedContent = encryptedStream.ToArray();

                        var path = @"uploads/" + $"{entryDbModel.guid}.niei";
                        await System.IO.File.WriteAllBytesAsync(path, encryptedContent);
                    }
                }
            }

            _context.Add(entryDbModel);
            await _context.SaveChangesAsync();

            var ipAddr = GetRequestIp(Request);
            _logger.LogInformation($"{ipAddr} created {entryDbModel.guid}");

            return RedirectToAction(nameof(Details), new {id = entryDbModel.guid });

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction(nameof(Index));
            }

            var entryDbModel = await _context.Entry.FirstOrDefaultAsync(x => x.guid == id.ToLower());

            if (string.IsNullOrWhiteSpace(entryDbModel?.guid))
            {
                return RedirectToAction(nameof(Index));
            }

            System.IO.File.Delete("uploads/" + entryDbModel.guid + ".niei");
            _context.Entry.Remove(entryDbModel);
            await _context.SaveChangesAsync();

            var ipAddr = GetRequestIp(Request);

            _logger.LogInformation($"{ipAddr} deleted {id}");

            return RedirectToAction(nameof(Index));
        }

        private static bool ContainsLetterAndNotJustNumbers(string input)
        {
            // Use a regular expression to check if the input contains at least one letter
            // and does not consist only of numbers.
            Regex regex = new Regex(@"[a-zA-Z]");
            return regex.IsMatch(input) && !Regex.IsMatch(input, @"^\d+$");
        }

        private static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        private static string SHA512(string input)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            using (var hash = System.Security.Cryptography.SHA512.Create())
            {
                var hashedInputBytes = hash.ComputeHash(bytes);

                // Convert to text
                // StringBuilder Capacity is 128, because 512 bits / 8 bits in byte * 2 symbols for byte 
                var hashedInputStringBuilder = new System.Text.StringBuilder(128);
                foreach (var b in hashedInputBytes)
                    hashedInputStringBuilder.Append(b.ToString("X2"));
                return hashedInputStringBuilder.ToString();
            }
        }

        private string GetRequestIp(HttpRequest request)
        {
            //Derived from https://developers.cloudflare.com/support/troubleshooting/restoring-visitor-ips/restoring-original-visitor-ips/
            const string HeaderKeyName = "CF-Connecting-IP";
            request.Headers.TryGetValue(HeaderKeyName, out StringValues headerValue);
            return headerValue.ToString() == "" ? "Development" : headerValue.ToString();
        }

        private string GetRequestCountry(HttpRequest request)
        {
            const string HeaderKeyName = "CF-IPCountry";
            request.Headers.TryGetValue(HeaderKeyName, out StringValues headerValue);
            return headerValue.ToString() == "" ? "Development" : headerValue.ToString();
        }
    }
}
