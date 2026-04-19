using System.ComponentModel.DataAnnotations;

namespace datn.Models
{
    public class Notification
    {
        public int Id { get; set; }

        public int? RecipientId { get; set; } // Nếu null là gửi cho tất cả
        public string? RecipientRole { get; set; } // Gửi theo Role (Manager, Employee, Parent)

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public string? Url { get; set; } // Link để nhấn vào xem chi tiết

        public string Type { get; set; } = "info"; // info, success, warning, error

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        // Navigation
        public Account? Recipient { get; set; }
    }
}
