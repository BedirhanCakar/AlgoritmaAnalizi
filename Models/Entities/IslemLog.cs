using System;
using System.ComponentModel.DataAnnotations;

namespace ExamScheduler.Models.Entities
{
    public class IslemLog
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string IslemTipi { get; set; } // INSERT, UPDATE, DELETE

        [Required, StringLength(50)]
        public string TabloAdi { get; set; }

        public int KayitId { get; set; }

        public string EskiDeger { get; set; }
        public string YeniDeger { get; set; }

        [StringLength(50)]
        public string YapanKullanici { get; set; }

        public DateTime IslemTarihi { get; set; } = DateTime.Now;
    }
}
