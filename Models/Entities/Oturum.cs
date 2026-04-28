using System.ComponentModel.DataAnnotations;

namespace ExamScheduler.Models.Entities
{
    public class Oturum
    {
        public int Id { get; set; }
        
        [Required, StringLength(50)]
        public string Tanim { get; set; } // Örn: Sabah-1

        public TimeSpan BaslangicSaat { get; set; }
        public TimeSpan BitisSaat { get; set; }

        public ICollection<Sinav> Sinavlar { get; set; } = new List<Sinav>();
    }
}
