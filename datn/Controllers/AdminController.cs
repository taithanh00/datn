using datn.Data;
using datn.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace datn.Controllers
{
    /// <summary>
    /// AdminController: API dùng cho testing và admin tasks
    /// CẢNH BÁO: Chỉ dùng trong development, phải bỏ đi khi deploy production
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// POST: /api/admin/test
        /// Tạo 3 test accounts cho 3 roles: Manager, Employee, Parent
        /// Mỗi account đều có mật khẩu: 123456
        /// 
        /// Response trả về JSON với thông tin credentials để test
        /// </summary>
        [HttpPost("test")]
        public async Task<IActionResult> CreateTestAccounts()
        {
            try
            {
                // Xóa dữ liệu test cũ nếu có
                var existingAccounts = await _context.Accounts
                    .Where(a => a.Username.StartsWith("test_"))
                    .ToListAsync();

                if (existingAccounts.Any())
                {
                    _context.Accounts.RemoveRange(existingAccounts);
                    await _context.SaveChangesAsync();
                }

                // Lấy các roles
                var roles = await _context.Roles.ToListAsync();
                var managerRole = roles.FirstOrDefault(r => r.Name == "Manager");
                var employeeRole = roles.FirstOrDefault(r => r.Name == "Employee");
                var parentRole = roles.FirstOrDefault(r => r.Name == "Parent");

                if (managerRole == null || employeeRole == null || parentRole == null)
                {
                    return BadRequest("Roles không tồn tại trong DB. Vui lòng chạy migration trước.");
                }

                // Mật khẩu test
                const string testPassword = "123456";
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(testPassword);

                var testAccounts = new List<Account>
                {
                    // Manager Account
                    new Account
                    {
                        Username = "test_manager",
                        Email = "test.manager@kindergarten.edu.vn",
                        PasswordHash = passwordHash,
                        PasswordSalt = "",
                        IsActive = true,
                        RoleId = managerRole.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    // Employee Account
                    new Account
                    {
                        Username = "test_employee",
                        Email = "test.employee@kindergarten.edu.vn",
                        PasswordHash = passwordHash,
                        PasswordSalt = "",
                        IsActive = true,
                        RoleId = employeeRole.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    // Parent Account
                    new Account
                    {
                        Username = "test_parent",
                        Email = "test.parent@kindergarten.edu.vn",
                        PasswordHash = passwordHash,
                        PasswordSalt = "",
                        IsActive = true,
                        RoleId = parentRole.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                _context.Accounts.AddRange(testAccounts);
                await _context.SaveChangesAsync();

                // Tạo Employee record cho Employee account
                var employeeAccount = testAccounts[1];
                var employee = new Employee
                {
                    AccountId = employeeAccount.Id,
                    FullName = "Test Employee",
                    Phone = "0912345678",
                    Position = "Giáo viên mầm non",
                    BaseSalary = 15000000m
                };
                _context.Employees.Add(employee);

                // Tạo Parent record cho Parent account
                var parentAccount = testAccounts[2];
                var parent = new Parent
                {
                    AccountId = parentAccount.Id,
                    FirstName = "Test",
                    LastName = "Parent",
                    Phone = "0987654321",
                    Address = "123 Test Street"
                };
                _context.Parents.Add(parent);

                await _context.SaveChangesAsync();

                // Trả về thông tin credentials
                var credentials = new
                {
                    success = true,
                    message = "✅ 3 test accounts đã được tạo thành công",
                    password = testPassword,
                    accounts = new[]
                    {
                        new
                        {
                            role = "Manager",
                            username = "test_manager",
                            email = "test.manager@kindergarten.edu.vn",
                            password = testPassword,
                            description = "👔 Quản lý hệ thống"
                        },
                        new
                        {
                            role = "Employee",
                            username = "test_employee",
                            email = "test.employee@kindergarten.edu.vn",
                            password = testPassword,
                            description = "👨‍🏫 Giáo viên"
                        },
                        new
                        {
                            role = "Parent",
                            username = "test_parent",
                            email = "test.parent@kindergarten.edu.vn",
                            password = testPassword,
                            description = "👨‍👩‍👧‍👦 Phụ huynh"
                        }
                    }
                };

                return Ok(credentials);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }

        /// <summary>
        /// GET: /api/admin/test
        /// Tự động tạo 3 test accounts và trả về credentials
        /// </summary>
        [HttpGet("test")]
        public async Task<IActionResult> GetTestInfo()
        {
            try
            {
                // Xóa dữ liệu test cũ nếu có
                var existingAccounts = await _context.Accounts
                    .Where(a => a.Username.StartsWith("test_"))
                    .ToListAsync();

                if (existingAccounts.Any())
                {
                    _context.Accounts.RemoveRange(existingAccounts);
                    await _context.SaveChangesAsync();
                }

                // Lấy các roles
                var roles = await _context.Roles.ToListAsync();
                var managerRole = roles.FirstOrDefault(r => r.Name == "Manager");
                var employeeRole = roles.FirstOrDefault(r => r.Name == "Employee");
                var parentRole = roles.FirstOrDefault(r => r.Name == "Parent");

                if (managerRole == null || employeeRole == null || parentRole == null)
                {
                    return BadRequest("Roles không tồn tại trong DB. Vui lòng chạy migration trước.");
                }

                // Mật khẩu test
                const string testPassword = "123456";
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(testPassword);

                var testAccounts = new List<Account>
                {
                    // Manager Account
                    new Account
                    {
                        Username = "test_manager",
                        Email = "test.manager@kindergarten.edu.vn",
                        PasswordHash = passwordHash,
                        PasswordSalt = "",
                        IsActive = true,
                        RoleId = managerRole.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    // Employee Account
                    new Account
                    {
                        Username = "test_employee",
                        Email = "test.employee@kindergarten.edu.vn",
                        PasswordHash = passwordHash,
                        PasswordSalt = "",
                        IsActive = true,
                        RoleId = employeeRole.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    // Parent Account
                    new Account
                    {
                        Username = "test_parent",
                        Email = "test.parent@kindergarten.edu.vn",
                        PasswordHash = passwordHash,
                        PasswordSalt = "",
                        IsActive = true,
                        RoleId = parentRole.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                _context.Accounts.AddRange(testAccounts);
                await _context.SaveChangesAsync();

                // Tạo Employee record cho Employee account
                var employeeAccount = testAccounts[1];
                var employee = new Employee
                {
                    AccountId = employeeAccount.Id,
                    FullName = "Test Employee",
                    Phone = "0912345678",
                    Position = "Giáo viên mầm non",
                    BaseSalary = 15000000m
                };
                _context.Employees.Add(employee);

                // Tạo Parent record cho Parent account
                var parentAccount = testAccounts[2];
                var parent = new Parent
                {
                    AccountId = parentAccount.Id,
                    FirstName = "Test",
                    LastName = "Parent",
                    Phone = "0987654321",
                    Address = "123 Test Street"
                };
                _context.Parents.Add(parent);

                await _context.SaveChangesAsync();

                // Trả về thông tin credentials
                var credentials = new
                {
                    success = true,
                    message = "✅ 3 test accounts đã được tạo thành công",
                    createdAt = DateTime.UtcNow,
                    password = testPassword,
                    note = "⚠️ Chỉ dùng trong development. Bỏ đi khi deploy production",
                    accounts = new[]
                    {
                        new
                        {
                            role = "Manager",
                            username = "test_manager",
                            email = "test.manager@kindergarten.edu.vn",
                            password = testPassword,
                            description = "👔 Quản lý hệ thống - Toàn quyền"
                        },
                        new
                        {
                            role = "Employee",
                            username = "test_employee",
                            email = "test.employee@kindergarten.edu.vn",
                            password = testPassword,
                            description = "👨‍🏫 Giáo viên - Quản lý lớp học"
                        },
                        new
                        {
                            role = "Parent",
                            username = "test_parent",
                            email = "test.parent@kindergarten.edu.vn",
                            password = testPassword,
                            description = "👨‍👩‍👧‍👦 Phụ huynh - Xem thông tin con"
                        }
                    },
                    usage = new
                    {
                        step1 = "Truy cập /Auth/Login",
                        step2 = "Nhập username và password từ trên",
                        step3 = "Bạn sẽ được vào dashboard tương ứng với role"
                    }
                };

                return Ok(credentials);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }
    }
}
