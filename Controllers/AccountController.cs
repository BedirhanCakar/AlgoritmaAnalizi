using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
            var user = await _context.Kullanicilar
                .FirstOrDefaultAsync(u => u.KullaniciAdi == KullaniciAdi && u.Sifre == Sifre);

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
