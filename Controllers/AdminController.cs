using ExamScheduler.Data;
using ExamScheduler.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ExamScheduler.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- BÖLÜMLER ---
        public async Task<IActionResult> Bolumler() => View(await _context.Bolumler.ToListAsync());
        
        [HttpPost]
        public async Task<IActionResult> BolumEkle(Bolum bolum)
        {
            _context.Bolumler.Add(bolum);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Bolumler));
        }

        // --- DERSLER ---
        public async Task<IActionResult> Dersler()
        {
            var dersler = await _context.Dersler.Include(d => d.Bolum).ToListAsync();
            ViewBag.Bolumler = new SelectList(_context.Bolumler, "Id", "Ad");
            return View(dersler);
        }

        [HttpPost]
        public async Task<IActionResult> DersEkle(Ders ders)
        {
            _context.Dersler.Add(ders);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Dersler));
        }

        // --- DERSLİKLER (SALONLAR) ---
        public async Task<IActionResult> Derslikler()
        {
            var derslikler = await _context.Derslikler.ToListAsync();
            return View(derslikler);
        }

        [HttpPost]
        public async Task<IActionResult> DerslikEkle(Derslik derslik)
        {
            _context.Derslikler.Add(derslik);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Derslikler));
        }

        // --- PERSONELLER ---
        public async Task<IActionResult> Personeller()
        {
            var personeller = await _context.Personeller.Include(p => p.Bolum).ToListAsync();
            ViewBag.Bolumler = new SelectList(_context.Bolumler, "Id", "Ad");
            return View(personeller);
        }

        [HttpPost]
        public async Task<IActionResult> PersonelEkle(Personel personel)
        {
            _context.Personeller.Add(personel);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Personeller));
        }

        // --- OTURUMLAR ---
        public async Task<IActionResult> Oturumlar()
        {
            var oturumlar = await _context.Oturumlar.ToListAsync();
            return View(oturumlar);
        }

        [HttpPost]
        public async Task<IActionResult> OturumEkle(Oturum oturum)
        {
            _context.Oturumlar.Add(oturum);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Oturumlar));
        }
        
        // --- SİLME İŞLEMLERİ ---
        [HttpPost]
        public async Task<IActionResult> Sil(string tur, int id)
        {
            try {
                switch (tur)
                {
                    case "Bolum": var b = await _context.Bolumler.FindAsync(id); if(b!=null) _context.Bolumler.Remove(b); break;
                    case "Ders": var d = await _context.Dersler.FindAsync(id); if(d!=null) _context.Dersler.Remove(d); break;
                    case "Derslik": var dl = await _context.Derslikler.FindAsync(id); if(dl!=null) _context.Derslikler.Remove(dl); break;
                    case "Personel": var p = await _context.Personeller.FindAsync(id); if(p!=null) _context.Personeller.Remove(p); break;
                    case "Oturum": var o = await _context.Oturumlar.FindAsync(id); if(o!=null) _context.Oturumlar.Remove(o); break;
                }
                await _context.SaveChangesAsync();
                TempData["Success"] = "Kayıt başarıyla silindi.";
            } catch {
                TempData["Error"] = "Bu kayda bağlı başka veriler olduğu için silinemedi.";
            }
            return RedirectToAction(tur + "ler");
        }
    }
}
