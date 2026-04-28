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

        public int OgrenciSayisi { get; set; }
        
        public int Yariyil { get; set; } // 1, 2, 3... vb.

        public int BolumId { get; set; }
        public Bolum Bolum { get; set; }

        public ICollection<Sinav> Sinavlar { get; set; } = new List<Sinav>();
    }
}
