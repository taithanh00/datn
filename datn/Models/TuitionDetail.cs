namespace datn.Models
{
    public class TuitionDetail
    {
        public int Id { get; set; }
        public int TuitionId { get; set; }
        public int? FeeItemId { get; set; }
        public int? SubjectId { get; set; }

        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal DiscountAmount { get; set; } = 0;
        public decimal TotalAmount { get; set; }

        public Tuition Tuition { get; set; } = null!;
        public FeeItem? FeeItem { get; set; }
        public Subject? Subject { get; set; }
    }
}
