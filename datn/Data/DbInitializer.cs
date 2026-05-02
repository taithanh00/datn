using datn.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace datn.Data
{
    public static class DbInitializer
    {
        public static async Task SeedManagerAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 1. Kiểm tra xem đã có tài khoản Manager nào chưa
            // RoleId = 1 là Manager (đã được seed trong AppDbContext)
            var managerExists = await context.Accounts.AnyAsync(a => a.RoleId == 1);

            if (!managerExists)
            {
                // 2. Tạo tài khoản Manager mặc định
                var adminAccount = new Account
                {
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Thanhbinh24!"),
                    Email = "admin@senhong.edu.vn",
                    RoleId = 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Accounts.Add(adminAccount);
                await context.SaveChangesAsync();

                // 3. Tạo thông tin Employee tương ứng để hiển thị Profile
                var adminEmployee = new Employee
                {
                    AccountId = adminAccount.Id,
                    FullName = "Hệ thống Quản trị",
                    Position = "Administrator",
                    IsActive = true
                };

                context.Employees.Add(adminEmployee);
                await context.SaveChangesAsync();
                
                Console.WriteLine("--> Seeded default Manager account: admin / Thanhbinh24!");
            }
        }
    }
}
