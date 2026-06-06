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
                    FirstName = "Hệ thống",
                    LastName = "Quản trị",
                    IsActive = true
                };

                context.Employees.Add(adminEmployee);
                await context.SaveChangesAsync();
                
                Console.WriteLine("--> Seeded default Manager account: admin / Thanhbinh24!");
            }
        }

        public static async Task SeedEducationAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 1. Subjects
            if (!await context.Subjects.AnyAsync())
            {
                var subjects = new List<Subject>
                {
                    new Subject { Name = "Toán tư duy", Description = "Làm quen với các con số và hình khối", IsActive = true },
                    new Subject { Name = "Tiếng Việt", Description = "Nhận diện bảng chữ cái", IsActive = true },
                    new Subject { Name = "Mỹ thuật", Description = "Tô màu và vẽ tranh sáng tạo", IsActive = true },
                    new Subject { Name = "Tiếng Anh (Bản ngữ)", Description = "Giao tiếp cơ bản với giáo viên nước ngoài", IsActive = true }
                };
                context.Subjects.AddRange(subjects);
                await context.SaveChangesAsync();
                Console.WriteLine("--> Seeded Subjects");
            }

            // 2. Employees (Teachers)
            if (await context.Employees.CountAsync() <= 1) // Only system admin exists
            {
                var teachers = new List<Employee>
                {
                    new Employee { FirstName = "Nguyễn Thị", LastName = "Lan", IsActive = true },
                    new Employee { FirstName = "Trần Văn", LastName = "Hùng", IsActive = true }
                };
                context.Employees.AddRange(teachers);
                await context.SaveChangesAsync();
                Console.WriteLine("--> Seeded Teachers");
            }

            var teacherLan = await context.Employees.FirstOrDefaultAsync(e => e.LastName == "Lan");
            var teacherHung = await context.Employees.FirstOrDefaultAsync(e => e.LastName == "Hung" || e.LastName == "Hùng");

            // 3. Classes
            if (!await context.Classes.AnyAsync())
            {
                var classes = new List<Class>
                {
                    new Class { Name = "Lớp Mầm 1", AgeFrom = 3, AgeTo = 4, SchoolYear = "2023-2024", LeadTeacherId = teacherLan?.Id, IsActive = true },
                    new Class { Name = "Lớp Chồi 2", AgeFrom = 4, AgeTo = 5, SchoolYear = "2023-2024", IsActive = true }
                };
                context.Classes.AddRange(classes);
                await context.SaveChangesAsync();
                Console.WriteLine("--> Seeded Classes");
            }

            var classMam1 = await context.Classes.FirstOrDefaultAsync(c => c.Name == "Lớp Mầm 1");
            var mathSubject = await context.Subjects.FirstOrDefaultAsync(s => s.Name == "Toán tư duy");

            // 5. Assignments
            if (!await context.Assignments.AnyAsync() && teacherHung != null && classMam1 != null)
            {
                context.Assignments.Add(new Assignment 
                { 
                    EmployeeId = teacherHung.Id, 
                    ClassId = classMam1.Id, 
                    StartDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(-1)),
                    RoleInClass = "Giáo viên phụ trách",
                    IsActive = true 
                });
                await context.SaveChangesAsync();
                Console.WriteLine("--> Seeded Assignments");
            }

            // 7. Class Schedule
            if (!await context.ClassSchedules.AnyAsync() && classMam1 != null && mathSubject != null)
            {
                context.ClassSchedules.Add(new ClassSchedule
                {
                    ClassId = classMam1.Id,
                    SubjectId = mathSubject.Id,
                    EmployeeId = null,
                    DayOfWeek = 1, // Monday
                    StartTime = new TimeOnly(8, 15),
                    EndTime = new TimeOnly(9, 45),
                    EffectiveFrom = DateOnly.FromDateTime(DateTime.Now.AddMonths(-1)),
                    Note = "Học tại phòng đa năng",
                    IsActive = true
                });
                await context.SaveChangesAsync();
                Console.WriteLine("--> Seeded ClassSchedules");
            }
        }
    }
}
