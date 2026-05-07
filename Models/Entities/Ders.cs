using System.ComponentModel.DataAnnotations;

namespace ExamScheduler.Models.Entities
{
    public class Ders
    {
        public int Id { get; set; }
        
        [Required, StringLength(20)]
        public string DersKodu { get; set; }

        [Required, StringLength(150)]
        public string Ad { get; set; }

        [Required, StringLength(50)]
        public string DersTuru { get; set; } // Zorunlu, Seçmeli

        [Range(1, int.MaxValue, ErrorMessage = "Öğrenci sayısı en az 1 olmalıdır.")]
        public int OgrenciSayisi { get; set; }
        
        [Range(1, 14, ErrorMessage = "Yarıyıl 1 ile 14 arasında olmalıdır.")]
        public int Yariyil { get; set; } // 1, 2, 3... vb.

        public int BolumId { get; set; }
        public Bolum Bolum { get; set; }

        public ICollection<Sinav> Sinavlar { get; set; } = new List<Sinav>();
    }
}
