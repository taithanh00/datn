namespace datn.Models
{
    public class WorkAttendance
    {
        public int EmployeeId { get; set; }
        public DateOnly Date { get; set; }
        public string? Status { get; set; }
        public string? Note { get; set; }

           public Employee Employee { get; set; }
    }
}
