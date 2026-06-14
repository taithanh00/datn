namespace datn.Models
{
    public class MonthlyStudentFeeAssignment
    {
        public int Id { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public int ClassId { get; set; }
        public int StudentId { get; set; }
        public int FeeItemId { get; set; }
        public decimal Amount { get; set; }
        public string? Note { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public Class Class { get; set; } = null!;
        public Student Student { get; set; } = null!;
        public FeeItem FeeItem { get; set; } = null!;
    }
}
