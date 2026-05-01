using System.ComponentModel.DataAnnotations;

namespace datn.Models
{
    public class FeeItem
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal DefaultAmount { get; set; }

        public int? AgeFrom { get; set; } // Giới hạn độ tuổi nếu có
        public int? AgeTo { get; set; }

        public bool IsRequired { get; set; } = false; // Bắt buộc hay Tùy chọn

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public ICollection<StudentFeeConfig> StudentFeeConfigs { get; set; } = new List<StudentFeeConfig>();
        public ICollection<TuitionDetail> TuitionDetails { get; set; } = new List<TuitionDetail>();
    }
}
