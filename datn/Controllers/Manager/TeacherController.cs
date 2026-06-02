using datn.Data;
using datn.Models;
using datn.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace datn.Controllers.Manager
{
    [Authorize(Roles = "Manager")]
    [Route("Manager")]
    public class TeacherController : BaseController
    {
        public TeacherController(AppDbContext context) : base(context) { }

        // ============ TEACHER API ============

        [HttpGet("Api/Teachers")]
        public async Task<IActionResult> GetTeachers(bool showInactive = false)
        {
            try
            {
                var query = _context.Employees.AsQueryable();
                
                if (showInactive)
                {
                    // Vượt rào để lấy các giáo viên đã nghỉ/khóa
                    query = query.IgnoreQueryFilters().Where(e => !e.IsActive);
                }

                var teachers = await query
                    .Include(e => e.Account)
                    .ThenInclude(a => a.Role)
                    .Where(e => e.Account.Role.Name == "Employee")
                    .OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
                    .ToListAsync();

                var result = teachers.Select(t => new
                {
                    id = t.Id,
                    fullName = t.FullName,
                    phone = t.Phone,
                    teacherType = t.TeacherType,
                    baseSalary = t.BaseSalary,
                    avatarPath = t.AvatarPath ?? "/images/lion_blue.png",
                    isActive = t.IsActive,
                    gender = t.Gender
                }).ToList();
                
                var total = result.Count;
                return Json(new { success = true, data = result, total });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("Api/Teacher/{id:int}")]
        public async Task<IActionResult> GetTeacher(int id)
        {
            var teacher = await _context.Employees
                .IgnoreQueryFilters()
                .Include(e => e.Account)
                .FirstOrDefaultAsync(e => e.Id == id);
                
            if (teacher == null)
                return Json(new { success = false, message = "Không tìm thấy giáo viên." });

            return Json(new
            {
                success = true,
                data = new
                {
                    id = teacher.Id,
                    firstName = teacher.FirstName,
                    lastName = teacher.LastName,
                    fullName = teacher.FullName,
                    email = teacher.Account?.Email,
                    username = teacher.Account?.Username,
                    gender = teacher.Gender,
                    phone = teacher.Phone,
                    teacherType = teacher.TeacherType,
                    baseSalary = teacher.BaseSalary,
                    avatarPath = teacher.AvatarPath,
                    isActive = teacher.Account?.IsActive ?? true,
                    bio = teacher.Bio,
                    qualifications = teacher.Qualifications,
                    experience = teacher.Experience,
                    philosophy = teacher.Philosophy,
                    specialty = teacher.Specialty,
                    showOnLanding = teacher.ShowOnLanding
                }
            });
        }

        [HttpPost("Api/Teacher")]
        public async Task<IActionResult> CreateTeacher([FromForm] CreateTeacherViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = errors });
            }
            if (await _context.Accounts.AnyAsync(a => a.Username == model.Username))
                return Json(new { success = false, message = "Tên đăng nhập đã tồn tại." });

            if (await _context.Accounts.AnyAsync(a => a.Email == model.Email))
                return Json(new { success = false, message = "Email đã tồn tại." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var role = await _context.Roles.FirstAsync(r => r.Name == "Employee");
                
                var account = new Account
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password.Trim()),
                    PasswordSalt = "",
                    RoleId = role.Id
                };
                _context.Accounts.Add(account);
                await _context.SaveChangesAsync();

                var teacher = new Employee
                {
                    AccountId = account.Id,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Gender = model.Gender,
                    Phone = model.Phone,
                    TeacherType = TeacherType.Lead,
                    BaseSalary = model.BaseSalary,
                    Bio = model.Bio,
                    Qualifications = model.Qualifications,
                    Experience = model.Experience,
                    Philosophy = model.Philosophy,
                    Specialty = model.Specialty,
                    ShowOnLanding = model.ShowOnLanding
                };
                if (model.Avatar != null) teacher.AvatarPath = await SaveAvatar(model.Avatar, "teacher");

                _context.Employees.Add(teacher);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return Json(new { success = true, message = "Thêm giáo viên thành công" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("Api/Teacher/{id:int}")]
        public async Task<IActionResult> UpdateTeacher(int id, [FromForm] UpdateTeacherViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = errors });
            }
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var teacher = await _context.Employees.Include(e => e.Account).FirstOrDefaultAsync(e => e.Id == id);
                if (teacher == null)
                    return Json(new { success = false, message = "Không tìm thấy giáo viên." });

                // Kiểm tra email trùng (loại trừ chính mình)
                if (await _context.Accounts.AnyAsync(a => a.Email == model.Email && a.Id != teacher.AccountId))
                    return Json(new { success = false, message = "Email này đã được sử dụng bởi tài khoản khác." });

                teacher.FirstName = model.FirstName;
                teacher.LastName = model.LastName;
                teacher.Gender = model.Gender;
                teacher.Phone = model.Phone;
                teacher.TeacherType = TeacherType.Lead;
                teacher.BaseSalary = model.BaseSalary;
                teacher.Bio = model.Bio;
                teacher.Qualifications = model.Qualifications;
                teacher.Experience = model.Experience;
                teacher.Philosophy = model.Philosophy;
                teacher.Specialty = model.Specialty;
                teacher.ShowOnLanding = model.ShowOnLanding;
                
                if (teacher.Account != null)
                {
                    teacher.Account.Email = model.Email;
                }

                if (model.Avatar != null) teacher.AvatarPath = await SaveAvatar(model.Avatar, "teacher");

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Json(new { success = true, message = "Cập nhật giáo viên thành công" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("Api/Teacher/{id:int}")]
        public async Task<IActionResult> DeactivateTeacher(int id)
        {
            try
            {
                var teacher = await _context.Employees.Include(e => e.Account).FirstOrDefaultAsync(e => e.Id == id);
                if (teacher == null)
                    return Json(new { success = false, message = "Không tìm thấy giáo viên." });

                // Vô hiệu hóa ở cả 2 cấp độ: Employee và Account
                teacher.IsActive = false;
                if (teacher.Account != null)
                {
                    teacher.Account.IsActive = false;
                    
                    // Thu hồi toàn bộ Refresh Token để đẩy giáo viên ra khỏi hệ thống
                    var activeTokens = await _context.RefreshTokens
                        .Where(r => r.AccountId == teacher.AccountId && !r.IsRevoked)
                        .ToListAsync();
                    
                    foreach (var token in activeTokens)
                    {
                        token.IsRevoked = true;
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã vô hiệu hóa giáo viên thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("Api/Teacher/Reactivate/{id:int}")]
        public async Task<IActionResult> ReactivateTeacher(int id)
        {
            try
            {
                // Sử dụng IgnoreQueryFilters để tìm bản ghi đang bị ẩn
                var teacher = await _context.Employees.IgnoreQueryFilters()
                    .Include(e => e.Account)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (teacher == null)
                    return Json(new { success = false, message = "Không tìm thấy giáo viên." });

                teacher.IsActive = true;
                if (teacher.Account != null)
                {
                    teacher.Account.IsActive = true;
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã khôi phục giáo viên thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task<string> SaveAvatar(IFormFile file, string prefix)
        {
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
            Directory.CreateDirectory(folderPath);

            var fileName = $"{prefix}_{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
            var path = Path.Combine(folderPath, fileName);
            await using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/uploads/avatars/{fileName}";
        }
    }
}

