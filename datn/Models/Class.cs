namespace datn.Models
{
    public class Class
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? AgeFrom { get; set; }
        public int? AgeTo { get; set; }
        public string? SchoolYear { get; set; }

        public ICollection<Student> Students { get; set; } = new List<Student>();
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        public ICollection<ClassActivity> ClassActivities { get; set; } = new List<ClassActivity>();
        public ICollection<TeachingPlan> TeachingPlans { get; set; } = new List<TeachingPlan>();
        public ICollection<ClassSchedule> ClassSchedules { get; set; } = new List<ClassSchedule>();
    }
}
