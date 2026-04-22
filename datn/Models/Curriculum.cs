namespace datn.Models
{
    public class Curriculum
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Content { get; set; }
        public int? SubjectId { get; set; }
        public int? AgeFrom { get; set; }
        public int? AgeTo { get; set; }

        public Subject? Subject { get; set; }
        public ICollection<TeachingPlan> TeachingPlans { get; set; } = new List<TeachingPlan>();
    }
}