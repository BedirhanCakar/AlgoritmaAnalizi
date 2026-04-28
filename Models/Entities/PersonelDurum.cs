using System;
using System.ComponentModel.DataAnnotations;

namespace ExamScheduler.Models.Entities
{
    public class PersonelDurum
    {
        public int Id { get; set; }

        public int PersonelId { get; set; }
        public Personel Personel { get; set; }

        public DateTime Tarih { get; set; }

        [StringLength(100)]
        public string MazeretTuru { get; set; } // Danışmanlık, İzinli vb.

        public bool Uygun { get; set; } = false;
    }
}
