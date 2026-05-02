namespace datn.Models
{
    public class HealthRecord
    {
        public int StudentId { get; set; }
        public DateOnly Date { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Height { get; set; }
        public decimal? Temperature { get; set; } // Nhiệt độ cơ thể
        public string? Note { get; set; }

        public Student Student { get; set; }
    }
}
