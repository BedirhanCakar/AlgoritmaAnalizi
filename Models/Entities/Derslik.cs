using System.ComponentModel.DataAnnotations;

namespace ExamScheduler.Models.Entities
{
    public class Derslik
    {
        public int Id { get; set; }
        
        [Required, StringLength(100)]
        public string Ad { get; set; }

        public int Kapasite { get; set; }
        
        [Required, StringLength(50)]
        public string Tip { get; set; } // Büyük Amfi, Sınıf, Lab vb.

        public bool Aktif { get; set; } = true;

        public ICollection<SinavSalonu> SinavSalonlari { get; set; } = new List<SinavSalonu>();
    }
}
