namespace datn.Models
{
    public class TuitionDetail
    {
        public int Id { get; set; }
        public int TuitionId { get; set; }
        public int? FeeItemId { get; set; }
        public int? SubjectId { get; set; } // Nếu là phí môn năng khiếu

        public string Name { get; set; } = string.Empty; // Lưu tên phí tại thời điểm xuất hóa đơn
        public decimal Amount { get; set; } // Giá gốc
        public decimal DiscountAmount { get; set; } = 0;
        public decimal TotalAmount { get; set; } // Giá sau cùng

        public Tuition Tuition { get; set; } = null!;
        public FeeItem? FeeItem { get; set; }
        public Subject? Subject { get; set; }
    }
}
