namespace datn.Models
{
    public class Tuition
    {
        public int Id { get; set; }
        public int? StudentId { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
        public int? TuitionPlanId { get; set; }
        public decimal? ExtraFee { get; set; }
        public bool IsPaid { get; set; } = false;

        public Student? Student { get; set; }
        public TuitionPlan? TuitionPlan { get; set; }
    }
}
