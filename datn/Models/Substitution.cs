using System;

namespace datn.Models
{
    public class Substitution
    {
        public int Id { get; set; }
        public int ClassScheduleId { get; set; }
        public DateOnly Date { get; set; }
        public int OriginalEmployeeId { get; set; }
        public int SubstituteEmployeeId { get; set; }
        public string? Note { get; set; }
        public string Status { get; set; } = "Confirmed"; // Confirmed, Cancelled
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public ClassSchedule ClassSchedule { get; set; } = null!;
        public Employee OriginalEmployee { get; set; } = null!;
        public Employee SubstituteEmployee { get; set; } = null!;
    }
}
