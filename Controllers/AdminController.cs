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
        
        // --- AKILLI SALON ATAMA (SINAVLAR) ---
        public async Task<IActionResult> AkilliAtama()
        {
            var sinavlar = await _context.Sinavlar
                .Include(s => s.Ders)
                .Include(s => s.Oturum)
                .Include(s => s.Salonlar).ThenInclude(ss => ss.Derslik)
                .ToListAsync();

            ViewBag.Dersler = new SelectList(_context.Dersler, "Id", "Ad");
            ViewBag.Oturumlar = new SelectList(_context.Oturumlar, "Id", "Tanim");
            return View(sinavlar);
        }

        [HttpPost]
        public async Task<IActionResult> SinavEkle(Sinav sinav)
        {
            _context.Sinavlar.Add(sinav);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(AkilliAtama));
        }

        [HttpPost]
        public async Task<IActionResult> SalonAta(int id)
        {
            try
            {
                // Stored Procedure Çağrısı (Cursor ile otomatik dağıtım)
                await _context.Database.ExecuteSqlRawAsync("EXEC SP_AkilliSalonHesapla @p0", id);
                TempData["Success"] = "Akıllı atama algoritması başarıyla çalıştırıldı ve salonlar dağıtıldı.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Atama sırasında bir hata oluştu: " + ex.Message;
            }
            return RedirectToAction(nameof(AkilliAtama));
        }

        // --- HAVUZ SİSTEMİ VE GÖZETMEN ATAMA (MODÜL 3) ---
        public async Task<IActionResult> HavuzSistemi()
        {
            // Havuzdaki personelin yük durumu
            var personelYuku = await _context.Personeller
                .Select(p => new
                {
                    Personel = p,
                    GorevSayisi = _context.GozetmenAtamalari.Count(ga => ga.PersonelId == p.Id)
                })
                .OrderBy(x => x.GorevSayisi)
                .ToListAsync();

            ViewBag.PersonelYuku = personelYuku;

            // Atanmış sınav salonları ve gözetmen durumları
            var sinavSalonlari = await _context.SinavSalonlari
                .Include(ss => ss.Sinav).ThenInclude(s => s.Ders)
                .Include(ss => ss.Sinav).ThenInclude(s => s.Oturum)
                .Include(ss => ss.Derslik)
                .Include(ss => ss.Gozetmenler).ThenInclude(ga => ga.Personel)
                .ToListAsync();

            return View(sinavSalonlari);
        }

        [HttpPost]
        public async Task<IActionResult> OtomatikGozetmenAta()
        {
            try
            {
                // Gözetmeni olmayan salonları bul
                var atanmamisSalonlar = await _context.SinavSalonlari
                    .Where(ss => !ss.Gozetmenler.Any())
                    .ToListAsync();

                int atananSayisi = 0;
                foreach (var salon in atanmamisSalonlar)
                {
                    // SP Çağrısı: En az görev alan uygun personeli atar
                    await _context.Database.ExecuteSqlRawAsync("EXEC SP_GozetmenHavuzAtama @p0", salon.Id);
                    atananSayisi++;
                }

                TempData["Success"] = $"{atananSayisi} adet salona havuz sisteminden otomatik gözetmen ataması yapıldı.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Gözetmen ataması sırasında bir hata oluştu: " + ex.Message;
            }
            return RedirectToAction(nameof(HavuzSistemi));
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
                    case "Sinav": var s = await _context.Sinavlar.FindAsync(id); if(s!=null) _context.Sinavlar.Remove(s); break;
                }
                await _context.SaveChangesAsync();
                TempData["Success"] = "Kayıt başarıyla silindi.";
            } catch {
                TempData["Error"] = "Bu kayda bağlı başka veriler olduğu için silinemedi.";
            }
            
            if (tur == "Sinav") return RedirectToAction(nameof(AkilliAtama));
            return RedirectToAction(tur + "ler");
        }
        // --- İŞLEM LOGLARI ---
        public async Task<IActionResult> Loglar()
        {
            var loglar = await _context.IslemLoglari.OrderByDescending(l => l.IslemTarihi).ToListAsync();
            return View(loglar);
        }

        // --- VERİTABANI YEDEKLEME ---
        [HttpPost]
        public async Task<IActionResult> YedekAl()
        {
            try
            {
                // Docker içerisindeki SQL Server'da /var/opt/mssql/data/ dizinine yedek alır.
                string backupPath = "/var/opt/mssql/data";
                await _context.Database.ExecuteSqlRawAsync("EXEC SP_YedekAl @p0", backupPath);
                TempData["Success"] = "Veritabanı başarıyla yedeklendi. (Konum: Docker içerisindeki /var/opt/mssql/data/ dizini)";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Yedekleme sırasında bir hata oluştu: " + ex.Message;
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
