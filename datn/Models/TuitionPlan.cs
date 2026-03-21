namespace datn.Models
{
    public class TuitionPlan
    {
        public int Id { get; set; }
        public int? AgeFrom { get; set; }
        public int? AgeTo { get; set; }
        public decimal? Amount { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }

        public ICollection<Tuition> Tuitions { get; set; }
    }
}
