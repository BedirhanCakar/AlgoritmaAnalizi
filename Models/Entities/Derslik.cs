using System.ComponentModel.DataAnnotations;

namespace ExamScheduler.Models.Entities
{
    public class Derslik
    {
        public int Id { get; set; }
        
        [Required, StringLength(100)]
        public string Ad { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Kapasite en az 1 olmalıdır.")]
        public int Kapasite { get; set; }
        
        [Required, StringLength(50)]
        public string Tip { get; set; } // Büyük Amfi, Sınıf, Lab vb.

        [Range(0, 20, ErrorMessage = "Kat bilgisi 0 ile 20 arasında olmalıdır.")]
        public int Kat { get; set; } = 0; // Kat bilgisi (0: Zemin, 1: 1. Kat vb.)

        public bool Aktif { get; set; } = true;

        public ICollection<SinavSalonu> SinavSalonlari { get; set; } = new List<SinavSalonu>();
    }
}
