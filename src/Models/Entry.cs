using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace FileSharing.Models
{
    [Index(nameof(customId), IsUnique = true)]
    public class Entry
    {
        [Key]
        public string guid { get; set; }

        [Required]
        public string originalFileName { get; set; }
        
        [Required]
        public byte[] iv { get; set; }
        
        [Required]
        public byte[] aesKey { get; set; }
        
        [MaxLength(16)]
        [MinLength(1)]
        public string? customId { get; set; }
        
        [Required]
        public DateTime? expiresAt { get; set; }

        [Required]
        public int maxNumOfDownloads { get; set; }
    }
}
