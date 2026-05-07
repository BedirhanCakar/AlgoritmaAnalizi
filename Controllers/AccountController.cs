using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using ExamScheduler.Data;
using ExamScheduler.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExamScheduler.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            // Eğer zaten giriş yapmışsa ana sayfaya yönlendir
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string KullaniciAdi, string Sifre)
        {
            string hashedSifre = HashPassword(Sifre);
            var user = await _context.Kullanicilar
                .FirstOrDefaultAsync(u => u.KullaniciAdi == KullaniciAdi && u.Sifre == hashedSifre);

            if (user == null)
            {
                TempData["Error"] = "Kullanıcı adı veya şifre hatalı.";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.KullaniciAdi),
                new Claim(ClaimTypes.Role, user.Rol)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, 
                new ClaimsPrincipal(claimsIdentity));

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(Kullanici model)
        {
            if (await _context.Kullanicilar.AnyAsync(u => u.KullaniciAdi == model.KullaniciAdi))
            {
                TempData["Error"] = "Bu kullanıcı adı zaten alınmış.";
                return View(model);
            }

            // Şifre kuralları kontrolü (En az 8 karakter, 1 büyük harf, 1 küçük harf, 1 rakam)
            if (!Regex.IsMatch(model.Sifre, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$"))
            {
                TempData["Error"] = "Şifreniz en az 8 karakter uzunluğunda olmalı, en az 1 büyük harf, 1 küçük harf ve 1 rakam içermelidir.";
                return View(model);
            }

            model.Sifre = HashPassword(model.Sifre);

            _context.Kullanicilar.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Kayıt başarılı! Lütfen giriş yapın.";
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}

