namespace datn.Models
{
    public enum SalaryStatus
    {
        Draft = 0,
        Calculated = 1,
        Locked = 2,
        Paid = 3,
        Cancelled = 4
    }

    public class Salary
    {
        public int EmployeeId { get; set; }
        public int PayrollPeriodId { get; set; }
        public decimal? WorkingDays { get; set; }
        public decimal? SalaryAmount { get; set; }
        public SalaryStatus Status { get; set; } = SalaryStatus.Draft;
        public decimal? BaseSalarySnapshot { get; set; }
        public int? StandardWorkingDays { get; set; }
        public decimal PenaltyAmount { get; set; }
        public decimal CoverageBonusAmount { get; set; }
        public DateTime? CalculatedAtUtc { get; set; }
        public DateTime? LockedAtUtc { get; set; }
        public DateTime? PaidAtUtc { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentNote { get; set; }

        public Employee Employee { get; set; }
        public PayrollPeriod PayrollPeriod { get; set; }
    }
}
