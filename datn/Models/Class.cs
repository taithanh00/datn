namespace datn.Models
{
    public class Class
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? AgeFrom { get; set; }
        public int? AgeTo { get; set; }
        public string? SchoolYear { get; set; }
        public int MaxCapacity { get; set; } = 25;
        public bool IsActive { get; set; } = true;
        public int? LeadTeacherId { get; set; } // GVCN duy nhất của lớp

        public ICollection<Student> Students { get; set; } = new List<Student>();
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        public ICollection<ClassActivity> ClassActivities { get; set; } = new List<ClassActivity>();
        public ICollection<ClassSchedule> ClassSchedules { get; set; } = new List<ClassSchedule>();
        public Employee? LeadTeacher { get; set; }
    }
}
