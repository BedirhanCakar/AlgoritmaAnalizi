using System.ComponentModel.DataAnnotations;

namespace ExamScheduler.Models.Entities
{
    public class Personel
    {
        public int Id { get; set; }

        [StringLength(50)]
        public string Unvan { get; set; }

        [Required, StringLength(50)]
        public string Ad { get; set; }

        [Required, StringLength(50)]
        public string Soyad { get; set; }

        public int BolumId { get; set; }
        public Bolum Bolum { get; set; }

        public ICollection<PersonelDurum> Durumlar { get; set; } = new List<PersonelDurum>();
        public ICollection<GozetmenAtama> Atamalar { get; set; } = new List<GozetmenAtama>();
    }
}
