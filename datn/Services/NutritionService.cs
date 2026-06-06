using datn.Data;
using datn.Models;
using Microsoft.EntityFrameworkCore;

namespace datn.Services
{
    public class NutritionService : INutritionService
    {
        private readonly AppDbContext _context;

        public NutritionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Menu>> GetWeeklyMenuAsync()
        {
            return await _context.Menus
                .Where(m => m.DayOfWeek >= 1 && m.DayOfWeek <= 6 && m.IsActive)
                .OrderBy(m => m.DayOfWeek)
                .ThenBy(m => m.MealType)
                .ToListAsync();
        }

        public async Task<bool> SaveMenuAsync(Menu menu)
        {
            if (menu.DayOfWeek < 1 || menu.DayOfWeek > 6)
            {
                return false;
            }

            menu.Ingredients = null;
            if (menu.Date == default)
            {
                menu.Date = DateOnly.FromDateTime(DateTime.Today);
            }

            Menu savedMenu;
            if (menu.Id == 0)
            {
                var existing = await _context.Menus
                    .FirstOrDefaultAsync(m => m.DayOfWeek == menu.DayOfWeek && m.MealType == menu.MealType);

                if (existing == null)
                {
                    _context.Menus.Add(menu);
                    savedMenu = menu;
                }
                else
                {
                    ApplyMenuValues(existing, menu);
                    savedMenu = existing;
                }
            }
            else
            {
                var existing = await _context.Menus.FindAsync(menu.Id);
                if (existing == null) return false;

                ApplyMenuValues(existing, menu);
                savedMenu = existing;
            }

            await DeactivateDuplicateMenusAsync(savedMenu);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteMenuAsync(int id)
        {
            var menu = await _context.Menus.FindAsync(id);
            if (menu == null) return false;

            menu.IsActive = false;
            return await _context.SaveChangesAsync() > 0;
        }

        private static void ApplyMenuValues(Menu target, Menu source)
        {
            target.DayOfWeek = source.DayOfWeek;
            target.Date = source.Date;
            target.MealType = source.MealType;
            target.DishName = source.DishName;
            target.Ingredients = null;
            target.Note = source.Note;
            target.IsActive = true;
        }

        private async Task DeactivateDuplicateMenusAsync(Menu menu)
        {
            var duplicateMenus = await _context.Menus
                .Where(m => m.Id != menu.Id &&
                            m.DayOfWeek == menu.DayOfWeek &&
                            m.MealType == menu.MealType &&
                            m.IsActive)
                .ToListAsync();

            foreach (var duplicate in duplicateMenus)
            {
                duplicate.IsActive = false;
            }
        }
    }
}
