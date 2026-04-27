namespace datn.Models
{
    public class Salary
    {
        public int EmployeeId { get; set; }
        public int PayrollPeriodId { get; set; }
        public decimal? WorkingDays { get; set; }
        public decimal? SalaryAmount { get; set; }

        public Employee Employee { get; set; }
        public PayrollPeriod PayrollPeriod { get; set; }
    }
}
