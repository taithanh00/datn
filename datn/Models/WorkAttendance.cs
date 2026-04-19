namespace datn.Models
{
    public class WorkAttendance
    {
        public int EmployeeId { get; set; }
        public DateOnly Date { get; set; }
        public DateTime? CheckInAtUtc { get; set; }
        public DateTime? CheckOutAtUtc { get; set; }
        public int? WorkedMinutes { get; set; }
        public decimal? WorkUnit { get; set; }
        public bool IsLate { get; set; }
        public decimal PenaltyAmount { get; set; }
        public string Status { get; set; } = "Pending";
        public string? Note { get; set; }
        public int? ReviewedByEmployeeId { get; set; }
        public DateTime? ReviewedAtUtc { get; set; }
        public string? ReviewNote { get; set; }

        public Employee Employee { get; set; }
    }
}
