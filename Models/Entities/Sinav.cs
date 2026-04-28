using System;
using System.ComponentModel.DataAnnotations;

namespace ExamScheduler.Models.Entities
{
    public class Sinav
    {
        public int Id { get; set; }

        public int DersId { get; set; }
        public Ders Ders { get; set; }

        public DateTime Tarih { get; set; }

        public int OturumId { get; set; }
        public Oturum Oturum { get; set; }

        public ICollection<SinavSalonu> Salonlar { get; set; } = new List<SinavSalonu>();
    }
}
