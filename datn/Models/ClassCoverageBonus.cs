namespace datn.Models
{
    public class ClassCoverageBonus
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public int ClassId { get; set; }
        public DateOnly Date { get; set; }
        public int AbsentEmployeeId { get; set; }
        public int? LeaveRequestId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public string? Note { get; set; }

        public Employee Employee { get; set; } = null!;
        public Class Class { get; set; } = null!;
        public Employee AbsentEmployee { get; set; } = null!;
    }
}
