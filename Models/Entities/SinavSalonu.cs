namespace ExamScheduler.Models.Entities
{
    public class SinavSalonu
    {
        public int Id { get; set; }

        public int SinavId { get; set; }
        public Sinav Sinav { get; set; }

        public int DerslikId { get; set; }
        public Derslik Derslik { get; set; }

        public ICollection<GozetmenAtama> Gozetmenler { get; set; } = new List<GozetmenAtama>();
    }
}
