using System.ComponentModel.DataAnnotations;

namespace ExamScheduler.Models.Entities
{
    public class Bolum
    {
        public int Id { get; set; }
        
        [Required, StringLength(100)]
        public string Ad { get; set; }

        public ICollection<Ders> Dersler { get; set; } = new List<Ders>();
        public ICollection<Personel> Personeller { get; set; } = new List<Personel>();
    }
}
