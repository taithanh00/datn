namespace datn.Models
{
    public class StudyReport
    {
        public int StudentId { get; set; }
        public DateOnly Date { get; set; }
        public int? RankingId { get; set; }
        public int? TeacherId { get; set; }
        public string? Comment { get; set; }

        public Student Student { get; set; }
        public Ranking? Ranking { get; set; }
        public Employee? Teacher { get; set; }
    }
}
