namespace ExamScheduler.Models.Entities
{
    public class GozetmenAtama
    {
        public int Id { get; set; }

        public int SinavSalonuId { get; set; }
        public SinavSalonu SinavSalonu { get; set; }

        public int PersonelId { get; set; }
        public Personel Personel { get; set; }
    }
}
