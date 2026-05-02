using datn.Models;

namespace datn.Services
{
    public interface INutritionService
    {
        Task<List<Menu>> GetWeeklyMenuAsync(DateOnly startDate);
        Task<bool> SaveMenuAsync(Menu menu);
        Task<bool> DeleteMenuAsync(int id);
        
        // Logic ghi đè (Override)
        Task<List<MenuOverride>> GetOverridesAsync(DateOnly date, int? classId = null);
        Task<bool> SaveOverrideAsync(MenuOverride menuOverride);
        Task<bool> DeleteOverrideAsync(int id);
        
        // Quét dị ứng tự động
        Task<int> AutoScanAllergiesAsync(int menuId);
        
        // Lấy thực đơn thực tế cho học sinh (đã tính đến override)
        Task<List<StudentMealViewModel>> GetDailyMenuForClassAsync(int classId, DateOnly date);
    }

    public class StudentMealViewModel
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string? Allergies { get; set; }
        public List<MealDetailViewModel> Meals { get; set; } = new();
    }

    public class MealDetailViewModel
    {
        public MealType MealType { get; set; }
        public string OriginalDish { get; set; }
        public string EffectiveDish { get; set; }
        public bool IsOverridden { get; set; }
        public string? OverrideReason { get; set; }
        public bool HasAllergyConflict { get; set; }
    }
}
