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
            if (await _context.Sinavlar.AnyAsync(s => s.DersId == sinav.DersId))
            {
                TempData["Error"] = "Bu dersin zaten planlanmış bir sınavı var, yenisini eklemek için önce mevcut olanı silmelisiniz.";
                return RedirectToAction(nameof(AkilliAtama));
            }

            // Dönem (Yarıyıl) Çakışması Kontrolü (UDF - FN_DersCakismasiVarMi çağrılarak)
            var cakisiyor = false;
            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "SELECT dbo.FN_DersCakismasiVarMi(@DersId, @OturumId, @Tarih)";
                
                var pDersId = command.CreateParameter();
                pDersId.ParameterName = "@DersId";
                pDersId.Value = sinav.DersId;
                command.Parameters.Add(pDersId);
                
                var pOturumId = command.CreateParameter();
                pOturumId.ParameterName = "@OturumId";
                pOturumId.Value = sinav.OturumId;
                command.Parameters.Add(pOturumId);
                
                var pTarih = command.CreateParameter();
                pTarih.ParameterName = "@Tarih";
                pTarih.Value = sinav.Tarih;
                command.Parameters.Add(pTarih);
                
                if (command.Connection.State != System.Data.ConnectionState.Open)
                    await command.Connection.OpenAsync();
                    
                var result = await command.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    cakisiyor = Convert.ToBoolean(result);
                }
            }

            if (cakisiyor)
            {
                TempData["Error"] = "Aynı yarıyılda yer alan derslerin sınavları aynı oturuma planlanamaz! (Dönem Çakışması Engellendi)";
                return RedirectToAction(nameof(AkilliAtama));
            }

            // Günlük Sınav Limiti Kontrolü (Yarıyıl başına günde en fazla 1 sınav)
            var yariyil = await _context.Dersler.Where(d => d.Id == sinav.DersId).Select(d => d.Yariyil).FirstOrDefaultAsync();
            var gunlukSinavSayisi = await _context.Sinavlar
                .CountAsync(s => s.Tarih.Date == sinav.Tarih.Date && s.Ders.Yariyil == yariyil);
            if (gunlukSinavSayisi >= 1)
            {
                TempData["Error"] = $"Hata: {yariyil}. Yarıyıl için {sinav.Tarih.ToString("dd.MM.yyyy")} tarihinde zaten planlanmış bir sınav bulunmaktadır! Aynı dönem için günde en fazla 1 sınav planlanabilir.";
                return RedirectToAction(nameof(AkilliAtama));
            }

            _context.Sinavlar.Add(sinav);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(AkilliAtama));
        }

        [HttpGet]
        public async Task<IActionResult> SinavGuncelle(int id)
        {
            var sinav = await _context.Sinavlar.Include(s => s.Ders).FirstOrDefaultAsync(s => s.Id == id);
            if (sinav == null) return NotFound();

            ViewBag.Oturumlar = new SelectList(_context.Oturumlar, "Id", "Tanim");
            return View(sinav);
        }

        [HttpPost]
        public async Task<IActionResult> SinavGuncelle(Sinav model)
        {
            var sinav = await _context.Sinavlar.FindAsync(model.Id);
            if (sinav == null) return NotFound();

            // Dönem (Yarıyıl) Çakışması Kontrolü (UDF - FN_DersCakismasiVarMi çağrılarak)
            var cakisiyor = false;
            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "SELECT dbo.FN_DersCakismasiVarMi(@DersId, @OturumId, @Tarih)";
                
                var pDersId = command.CreateParameter();
                pDersId.ParameterName = "@DersId";
                pDersId.Value = sinav.DersId;
                command.Parameters.Add(pDersId);
                
                var pOturumId = command.CreateParameter();
                pOturumId.ParameterName = "@OturumId";
                pOturumId.Value = model.OturumId;
                command.Parameters.Add(pOturumId);
                
                var pTarih = command.CreateParameter();
                pTarih.ParameterName = "@Tarih";
                pTarih.Value = model.Tarih;
                command.Parameters.Add(pTarih);
                
                if (command.Connection.State != System.Data.ConnectionState.Open)
                    await command.Connection.OpenAsync();
                    
                var result = await command.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    cakisiyor = Convert.ToBoolean(result);
                }
            }

            if (cakisiyor)
            {
                TempData["Error"] = "Aynı yarıyılda yer alan derslerin sınavları aynı oturuma planlanamaz! (Dönem Çakışması Engellendi)";
                return RedirectToAction(nameof(AkilliAtama));
            }

            // Günlük Sınav Limiti Kontrolü (Yarıyıl başına günde en fazla 1 sınav)
            var yariyil = await _context.Dersler.Where(d => d.Id == sinav.DersId).Select(d => d.Yariyil).FirstOrDefaultAsync();
            var gunlukSinavSayisi = await _context.Sinavlar
                .CountAsync(s => s.Tarih.Date == model.Tarih.Date && s.Ders.Yariyil == yariyil && s.Id != model.Id);
            if (gunlukSinavSayisi >= 1)
            {
                TempData["Error"] = $"Hata: {yariyil}. Yarıyıl için {model.Tarih.ToString("dd.MM.yyyy")} tarihinde zaten planlanmış bir sınav bulunmaktadır! Aynı dönem için günde en fazla 1 sınav planlanabilir.";
                return RedirectToAction(nameof(AkilliAtama));
            }

            sinav.OturumId = model.OturumId;
            sinav.Tarih = model.Tarih;
            
            _context.Sinavlar.Update(sinav);
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Sınav başarıyla güncellendi ve işlem loglara kaydedildi.";
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
            if (tur == "Oturum") return RedirectToAction(nameof(Oturumlar));
            return RedirectToAction(tur + "ler");
        }

        [HttpPost]
        public async Task<IActionResult> LogSil(int id)
        {
            var log = await _context.IslemLoglari.FindAsync(id);
            if (log != null)
            {
                _context.IslemLoglari.Remove(log);
                await _context.SaveChangesAsync();
                TempData["Success"] = "İşlem logu başarıyla silindi.";
            }
            else
            {
                TempData["Error"] = "Silinmek istenen log bulunamadı.";
            }
            return RedirectToAction(nameof(Loglar));
        }

        [HttpPost]
        public async Task<IActionResult> TumLoglariTemizle()
        {
            try
            {
                _context.IslemLoglari.RemoveRange(_context.IslemLoglari);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Tüm işlem logları başarıyla temizlendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Loglar temizlenirken bir hata oluştu: " + ex.Message;
            }
            return RedirectToAction(nameof(Loglar));
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
                // Veritabanı sunucusunun işletim sistemini öğrenelim
                var hostPlatform = "Windows";
                try
                {
                    using (var command = _context.Database.GetDbConnection().CreateCommand())
                    {
                        command.CommandText = "SELECT host_platform FROM sys.dm_os_host_info";
                        if (command.Connection.State != System.Data.ConnectionState.Open)
                            await command.Connection.OpenAsync();
                        var res = await command.ExecuteScalarAsync();
                        if (res != null) hostPlatform = res.ToString();
                    }
                }
                catch
                {
                    if (_context.Database.GetDbConnection().ConnectionString.Contains("Server=db", StringComparison.OrdinalIgnoreCase))
                    {
                        hostPlatform = "Linux";
                    }
                }

                string backupPath;
                if (hostPlatform.Equals("Linux", StringComparison.OrdinalIgnoreCase))
                {
                    backupPath = "/var/opt/mssql/backup";
                }
                else
                {
                    backupPath = @"C:\Yedekler";
                    if (!System.IO.Directory.Exists(backupPath))
                    {
                        System.IO.Directory.CreateDirectory(backupPath);
                    }
                }

                await _context.Database.ExecuteSqlRawAsync("EXEC SP_YedekAl @p0", backupPath);
                TempData["Success"] = $"Veritabanı başarıyla yedeklendi. Klasör: {backupPath}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Yedekleme sırasında bir hata oluştu: " + ex.Message;
            }
            return RedirectToAction("Index", "Home");
        }

        // --- MAZERET YÖNETİMİ ---
        public async Task<IActionResult> Mazeretler(int personelId)
        {
            var personel = await _context.Personeller
                .Include(p => p.Durumlar)
                .FirstOrDefaultAsync(p => p.Id == personelId);
                
            if (personel == null) return NotFound();
            
            return View(personel);
        }

        [HttpPost]
        public async Task<IActionResult> MazeretEkle(int PersonelId, DateTime Tarih, string MazeretTuru, bool Uygun)
        {
            // O gün görevi olan gözetmene o gün için mazeret girilmesini engelle
            bool hasDuty = await _context.GozetmenAtamalari
                .AnyAsync(ga => ga.PersonelId == PersonelId && ga.SinavSalonu.Sinav.Tarih.Date == Tarih.Date);

            if (hasDuty)
            {
                TempData["Error"] = "Hata: Bu personelin belirtilen tarihte aktif sınav görevi bulunmaktadır. Görevi olan personele mazeret tanımlanamaz.";
                return RedirectToAction(nameof(Mazeretler), new { personelId = PersonelId });
            }

            var durum = new PersonelDurum
            {
                PersonelId = PersonelId,
                Tarih = Tarih,
                MazeretTuru = MazeretTuru,
                Uygun = Uygun
            };
            
            _context.PersonelDurumlar.Add(durum);
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Mazeret başarıyla eklendi.";
            return RedirectToAction(nameof(Mazeretler), new { personelId = PersonelId });
        }

        [HttpPost]
        public async Task<IActionResult> MazeretSil(int id, int personelId)
        {
            var durum = await _context.PersonelDurumlar.FindAsync(id);
            if (durum != null)
            {
                _context.PersonelDurumlar.Remove(durum);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Mazeret silindi.";
            }
            return RedirectToAction(nameof(Mazeretler), new { personelId = personelId });
        }
    }
}
