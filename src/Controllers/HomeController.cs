﻿using Microsoft.AspNetCore.Mvc;
using FileSharing.Models;
using System.Diagnostics;

namespace FileSharing.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ChangeTheme(string theme)
        {
            if (theme == "light")
            {
                if (HttpContext.Request.Cookies.Any(x => x.Key == "__Secure-DarkMode" && x.Value == "false"))
                {
                    return Ok();
                }
                HttpContext.Response.Cookies.Delete("__Secure-DarkMode");
                var options = new CookieOptions
                {
                    Expires = DateTime.Now.AddMonths(12),
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    IsEssential = true,
                    Path = "/",
                    HttpOnly = true
                };
                HttpContext.Response.Cookies.Append("__Secure-DarkMode", "false", options);
            }
            else
            {
                if (HttpContext.Request.Cookies.Any(x => x.Key == "__Secure-DarkMode" && x.Value == "true"))
                {
                    return Ok();
                }
                else
                {
                    var options = new CookieOptions
                    {
                        Expires = DateTime.Now.AddMonths(12),
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        IsEssential = true,
                        Path = "/",
                        HttpOnly = true
                    };
                    HttpContext.Response.Cookies.Append("__Secure-DarkMode", "true", options);
                }

            }
            return Ok();
        }
    }
}