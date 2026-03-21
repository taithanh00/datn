namespace datn.Models
{
    public class Ranking
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        public ICollection<StudyReport> StudyReports { get; set; }
    }
}
