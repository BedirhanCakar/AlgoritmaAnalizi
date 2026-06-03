using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

using ExamScheduler.Data;
using ExamScheduler.Models.Entities;
using ExamScheduler.Services;
using Microsoft.EntityFrameworkCore;

namespace ExamScheduler.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public AccountController(ApplicationDbContext context, IEmailService emailService, IConfiguration configuration)
        {
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
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
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string KullaniciAdiOrEposta, string Sifre)
        {
            string hashedSifre = HashPassword(Sifre);
            
            // Kullanıcı adı veya e-posta eşleşmesi kontrolü
            var user = await _context.Kullanicilar
                .FirstOrDefaultAsync(u => (u.KullaniciAdi == KullaniciAdiOrEposta || u.Eposta == KullaniciAdiOrEposta) && u.Sifre == hashedSifre);

            if (user == null)
            {
                TempData["Error"] = "Kullanıcı adı/E-posta veya şifre hatalı.";
                return View();
            }

            // OTP Kodu Üret (6 Haneli)
            var random = new Random();
            string otpCode = random.Next(100000, 999999).ToString();

            // Session'a kaydet (User ID, Code, Expiration)
            HttpContext.Session.SetInt32("OtpUserId", user.Id);
            HttpContext.Session.SetString("OtpCode", otpCode);
            HttpContext.Session.SetString("OtpExpireTime", DateTime.Now.AddMinutes(3).ToString());

            // E-posta Gönder
            try
            {
                string subject = "Giris Dogrulama Kodu - Sinav Takip Sistemi";
                string body = $"<h3>Sınav Takip Sistemi Güvenlik Doğrulaması</h3>" +
                              $"<p>Giriş yapabilmek için doğrulama kodunuz: <strong style='font-size: 1.2rem; color: #0d6efd;'>{otpCode}</strong></p>" +
                              $"<p>Bu kod 3 dakika boyunca geçerlidir.</p>";

                await _emailService.SendEmailAsync(user.Eposta, subject, body);
                
                // SMTP ayarları yapılmadıysa test kolaylığı için kodu ekranda göster
                var emailPassword = _configuration["Email:SenderPassword"];
                if (string.IsNullOrEmpty(emailPassword) || emailPassword.Contains("BURAYA"))
                {
                    TempData["EmailMockCode"] = otpCode;
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Doğrulama kodu e-postası gönderilirken hata oluştu: " + ex.Message;
                return View();
            }

            return RedirectToAction(nameof(VerifyCode));
        }

        [HttpGet]
        public IActionResult VerifyCode()
        {
            var userId = HttpContext.Session.GetInt32("OtpUserId");
            var otpCode = HttpContext.Session.GetString("OtpCode");

            if (userId == null || string.IsNullOrEmpty(otpCode))
            {
                return RedirectToAction(nameof(Login));
            }

            if (TempData["EmailMockCode"] != null)
            {
                TempData.Keep("EmailMockCode");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyCode(string code)
        {
            var userId = HttpContext.Session.GetInt32("OtpUserId");
            var savedCode = HttpContext.Session.GetString("OtpCode");
            var expireTimeStr = HttpContext.Session.GetString("OtpExpireTime");

            if (userId == null || string.IsNullOrEmpty(savedCode) || string.IsNullOrEmpty(expireTimeStr))
            {
                TempData["Error"] = "Geçersiz işlem süresi. Lütfen tekrar giriş yapın.";
                return RedirectToAction(nameof(Login));
            }

            var expireTime = DateTime.Parse(expireTimeStr);
            if (DateTime.Now > expireTime)
            {
                TempData["Error"] = "Doğrulama kodunun süresi doldu (3 dakika). Lütfen tekrar giriş yapın.";
                return RedirectToAction(nameof(Login));
            }

            if (savedCode != code)
            {
                TempData["Error"] = "Hatalı doğrulama kodu!";
                if (TempData["EmailMockCode"] != null) TempData.Keep("EmailMockCode");
                return View();
            }

            var user = await _context.Kullanicilar.FindAsync(userId.Value);
            if (user == null)
            {
                TempData["Error"] = "Kullanıcı bulunamadı.";
                return RedirectToAction(nameof(Login));
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

            // Session temizle
            HttpContext.Session.Remove("OtpUserId");
            HttpContext.Session.Remove("OtpCode");
            HttpContext.Session.Remove("OtpExpireTime");

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

            // E-posta format kontrolü
            if (string.IsNullOrEmpty(model.Eposta) || !Regex.IsMatch(model.Eposta, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                TempData["Error"] = "Lütfen geçerli bir e-posta adresi giriniz.";
                return View(model);
            }

            // E-posta tekillik kontrolü
            if (await _context.Kullanicilar.AnyAsync(u => u.Eposta == model.Eposta))
            {
                TempData["Error"] = "Bu e-posta adresi ile kayıtlı bir kullanıcı zaten mevcut.";
                return View(model);
            }

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
