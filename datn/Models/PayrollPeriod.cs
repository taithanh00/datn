namespace datn.Models
{
    public class PayrollPeriod
    {
        public int Id { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
        public bool IsLocked { get; set; }
        public DateTime? LockedAtUtc { get; set; }

        public ICollection<Salary> Salaries { get; set; }
    }
}
