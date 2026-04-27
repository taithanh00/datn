namespace datn.Models
{
    public class EmployeeLeaveRequest
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public bool IsPaid { get; set; } = false;
        public string Status { get; set; } = "Pending";
        public string? ReviewNote { get; set; }
        public int? ReviewedByEmployeeId { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAtUtc { get; set; }

        public Employee Employee { get; set; } = null!;
    }
}
