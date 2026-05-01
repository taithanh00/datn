namespace datn.Models
{
    public class StudentFeeConfig
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int FeeItemId { get; set; }

        // Cho phép override giá mặc định cho từng học sinh nếu cần
        public decimal? CustomAmount { get; set; }

        public decimal DiscountAmount { get; set; } = 0;
        public decimal DiscountPercentage { get; set; } = 0;

        public string? Note { get; set; }

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public Student Student { get; set; } = null!;
        public FeeItem FeeItem { get; set; } = null!;
    }
}
