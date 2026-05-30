using datn.Models;

namespace datn.Services
{
    public interface INutritionService
    {
        Task<List<Menu>> GetWeeklyMenuAsync();
        Task<bool> SaveMenuAsync(Menu menu);
        Task<bool> DeleteMenuAsync(int id);
    }
}
