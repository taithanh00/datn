namespace datn.Models
{
    public class Class
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? AgeFrom { get; set; }
        public int? AgeTo { get; set; }
        public string? SchoolYear { get; set; }

        public ICollection<Student> Students { get; set; }
        public ICollection<Assignment> Assignments { get; set; }
        public ICollection<ClassActivity> ClassActivities { get; set; }
        public ICollection<TeachingPlan> TeachingPlans { get; set; }
    }
}
