namespace datn.Models
{
    public class MenuOverride
    {
        public int Id { get; set; }
        public int MenuId { get; set; }
        
        // Override có thể cho 1 bé hoặc cả lớp hoặc toàn trường (nếu cả 2 null)
        public int? StudentId { get; set; }
        public int? ClassId { get; set; }

        public string NewDishName { get; set; } = string.Empty;
        public string? Reason { get; set; } // Ví dụ: Dị ứng tôm, Đổi món lớp Lá 1
        public bool IsActive { get; set; } = true;

        public Menu Menu { get; set; }
        public Student? Student { get; set; }
        public Class? Class { get; set; }
    }
}
