namespace datn.Models
{
    public class TeachingPlan
    {
        public int ClassId { get; set; }
        public int CurriculumId { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string? Status { get; set; } // Planned, InProgress, Completed

        public Class Class { get; set; }
        public Curriculum Curriculum { get; set; }
    }
}
