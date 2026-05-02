using System.ComponentModel.DataAnnotations;

namespace datn.Models
{
    public class DailyReport
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public DateOnly Date { get; set; }

        // Ăn uống
        public EatingStatus EatingStatus { get; set; } = EatingStatus.Good;
        public string? EatingNote { get; set; }

        // Ngủ nghỉ
        public SleepingStatus SleepingStatus { get; set; } = SleepingStatus.Good;
        public string? SleepingNote { get; set; }

        // Vệ sinh & Sức khỏe
        public string? HygieneNote { get; set; }
        public string? HealthNote { get; set; } 

        // Hoạt động & Tâm trạng
        public string? ActivityNote { get; set; }
        public string? MoodNote { get; set; }
        
        public string? PhotoPaths { get; set; } // Lưu chuỗi JSON các đường dẫn ảnh

        public Student Student { get; set; }
    }

    public enum EatingStatus { Good, Normal, Poor }
    public enum SleepingStatus { Good, Normal, Poor, NoSleep }
}
