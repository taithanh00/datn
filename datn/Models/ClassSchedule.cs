namespace datn.Models
{
    public class ClassSchedule
    {
        public int Id { get; set; }
        public int ClassId { get; set; }
        public int SubjectId { get; set; }
        public int EmployeeId { get; set; }
        public int DayOfWeek { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public int? LocationId { get; set; }
        public DateOnly EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
        public string? Note { get; set; }
        public bool IsActive { get; set; } = true;

        public Class Class { get; set; } = null!;
        public Subject Subject { get; set; } = null!;
        public Employee Employee { get; set; } = null!;
        public Location? Location { get; set; }
    }
}
