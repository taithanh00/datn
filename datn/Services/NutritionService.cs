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

        public async Task<List<Menu>> GetWeeklyMenuAsync(DateOnly startDate)
        {
            var endDate = startDate.AddDays(7);
            return await _context.Menus
                .Include(m => m.MenuOverrides.Where(mo => mo.IsActive))
                    .ThenInclude(mo => mo.Student)
                .Where(m => m.Date >= startDate && m.Date < endDate && m.IsActive)
                .OrderBy(m => m.Date)
                .ThenBy(m => m.MealType)
                .ToListAsync();
        }

        public async Task<bool> SaveMenuAsync(Menu menu)
        {
            if (menu.Id == 0)
            {
                _context.Menus.Add(menu);
            }
            else
            {
                _context.Menus.Update(menu);
            }

            var result = await _context.SaveChangesAsync() > 0;
            
            // Tự động quét dị ứng sau khi lưu
            if (result && !string.IsNullOrWhiteSpace(menu.Ingredients))
            {
                await AutoScanAllergiesAsync(menu.Id);
            }

            return result;
        }

        public async Task<bool> DeleteMenuAsync(int id)
        {
            var menu = await _context.Menus.FindAsync(id);
            if (menu == null) return false;

            menu.IsActive = false; // Soft delete
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<List<MenuOverride>> GetOverridesAsync(DateOnly date, int? classId = null)
        {
            var query = _context.MenuOverrides
                .Include(mo => mo.Menu)
                .Include(mo => mo.Student)
                .Include(mo => mo.Class)
                .Where(mo => mo.Menu.Date == date);

            if (classId.HasValue)
            {
                query = query.Where(mo => mo.ClassId == classId || mo.Student.ClassId == classId);
            }

            return await query.ToListAsync();
        }

        public async Task<bool> SaveOverrideAsync(MenuOverride menuOverride)
        {
            if (menuOverride.Id == 0)
            {
                _context.MenuOverrides.Add(menuOverride);
            }
            else
            {
                _context.MenuOverrides.Update(menuOverride);
            }
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteOverrideAsync(int id)
        {
            var mo = await _context.MenuOverrides.FindAsync(id);
            if (mo == null) return false;

            mo.IsActive = false;
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<int> AutoScanAllergiesAsync(int menuId)
        {
            var menu = await _context.Menus.FindAsync(menuId);
            if (menu == null || string.IsNullOrWhiteSpace(menu.Ingredients)) return 0;

            // Tách danh sách các thành phần gây dị ứng từ thực đơn
            var menuAllergens = menu.Ingredients.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            
            // Lấy danh sách học sinh có thông tin dị ứng
            var studentsWithAllergies = await _context.Students
                .Where(s => s.Status == StudentStatus.Active && !string.IsNullOrEmpty(s.Allergies))
                .ToListAsync();

            int count = 0;
            foreach (var student in studentsWithAllergies)
            {
                // Kiểm tra xem bé có dị ứng với bất kỳ thành phần nào trong món ăn không
                bool hasConflict = menuAllergens.Any(a => student.Allergies!.Contains(a, StringComparison.OrdinalIgnoreCase));
                
                if (hasConflict)
                {
                    // Kiểm tra xem đã có bản ghi đè chưa để tránh tạo trùng
                    var existing = await _context.MenuOverrides
                        .AnyAsync(mo => mo.MenuId == menuId && mo.StudentId == student.Id);
                    
                    if (!existing)
                    {
                        _context.MenuOverrides.Add(new MenuOverride
                        {
                            MenuId = menuId,
                            StudentId = student.Id,
                            NewDishName = "[THAY THẾ] " + menu.DishName,
                            Reason = $"Dị ứng: {student.Allergies}",
                            IsActive = true
                        });
                        count++;
                    }
                }
            }

            if (count > 0) await _context.SaveChangesAsync();
            return count;
        }

        public async Task<List<StudentMealViewModel>> GetDailyMenuForClassAsync(int classId, DateOnly date)
        {
            var students = await _context.Students
                .Where(s => s.ClassId == classId && s.Status == StudentStatus.Active)
                .ToListAsync();

            var baseMenus = await _context.Menus
                .Where(m => m.Date == date && m.IsActive)
                .OrderBy(m => m.MealType)
                .ToListAsync();

            var overrides = await _context.MenuOverrides
                .Include(mo => mo.Menu)
                .Where(mo => mo.Menu.Date == date && mo.IsActive && (mo.ClassId == classId || mo.Student.ClassId == classId))
                .ToListAsync();

            var result = new List<StudentMealViewModel>();

            foreach (var student in students)
            {
                var vm = new StudentMealViewModel
                {
                    StudentId = student.Id,
                    StudentName = $"{student.FirstName} {student.LastName}",
                    Allergies = student.Allergies
                };

                foreach (var menu in baseMenus)
                {
                    // Tìm override cho bé này, hoặc cho lớp này
                    var specificOverride = overrides.FirstOrDefault(mo => mo.MenuId == menu.Id && mo.StudentId == student.Id);
                    var classOverride = overrides.FirstOrDefault(mo => mo.MenuId == menu.Id && mo.ClassId == classId);

                    var effectiveOverride = specificOverride ?? classOverride;

                    bool hasAllergyConflict = !string.IsNullOrEmpty(student.Allergies) && 
                                            !string.IsNullOrEmpty(menu.Ingredients) &&
                                            menu.Ingredients.Split(',').Any(a => student.Allergies.Contains(a.Trim(), StringComparison.OrdinalIgnoreCase));

                    vm.Meals.Add(new MealDetailViewModel
                    {
                        MealType = menu.MealType,
                        OriginalDish = menu.DishName,
                        EffectiveDish = effectiveOverride?.NewDishName ?? menu.DishName,
                        IsOverridden = effectiveOverride != null,
                        OverrideReason = effectiveOverride?.Reason,
                        HasAllergyConflict = hasAllergyConflict
                    });
                }
                result.Add(vm);
            }

            return result;
        }
    }
}
