namespace datn.Models
{
    public class Assignment
    {
        public int EmployeeId { get; set; }
        public int ClassId { get; set; }
        public DateOnly StartDate { get; set; }
        public string? RoleInClass { get; set; }
        public DateOnly? EndDate { get; set; }

        public Employee Employee { get; set; }
        public Class Class { get; set; }
    }
}
