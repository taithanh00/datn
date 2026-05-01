namespace datn.Models
{
    public class Tuition
    {
        public int Id { get; set; }
        public int? StudentId { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
        public decimal? ExtraFee { get; set; }
        public bool IsPaid { get; set; } = false;

        // Payment Gateway fields
        public string? PaymentMethod { get; set; }
        public string? TransactionId { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? PaymentStatus { get; set; }

        public Student? Student { get; set; }
        public ICollection<TuitionDetail> TuitionDetails { get; set; } = new List<TuitionDetail>();
    }
}
