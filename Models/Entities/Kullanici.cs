using System.ComponentModel.DataAnnotations;

namespace ExamScheduler.Models.Entities
{
    public class Kullanici
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string KullaniciAdi { get; set; }

        [Required, StringLength(255)]
        public string Sifre { get; set; } // Gerçek projelerde hashlenmeli!

        [Required, StringLength(50)]
        public string Rol { get; set; } // Admin veya Viewer
    }
}
