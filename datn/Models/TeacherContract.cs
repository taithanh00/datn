namespace datn.Models
{
    public enum TeacherContractStatus
    {
        Draft,
        Active,
        Expired,
        Terminated,
        Cancelled
    }

    public class TeacherContract
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string ContractType { get; set; } = string.Empty;
        public DateOnly SignedDate { get; set; }
        public DateOnly EffectiveDate { get; set; }
        public DateOnly? ExpiryDate { get; set; }
        public decimal? AgreedSalary { get; set; }
        public string? WorkPosition { get; set; }
        public string? WorkLocation { get; set; }
        public string? WorkingHours { get; set; }
        public TeacherContractStatus Status { get; set; } = TeacherContractStatus.Draft;
        public DateOnly? TerminationDate { get; set; }
        public string? TerminationReason { get; set; }
        public string? Note { get; set; }
        public string? OriginalFileName { get; set; }
        public string? StoredFileName { get; set; }
        public string? ContentType { get; set; }
        public long? FileSize { get; set; }
        public DateTime? UploadedAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public Employee Employee { get; set; } = null!;
    }
}
