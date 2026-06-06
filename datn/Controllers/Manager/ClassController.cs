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
    public class ClassController : BaseController
    {
        public ClassController(AppDbContext context) : base(context) { }

        // ============ CLASS MANAGEMENT API ============

        [HttpGet("Api/Classes")]
        public async Task<IActionResult> GetClasses(bool showInactive = false)
        {
            var query = _context.Classes.AsQueryable();
            if (showInactive)
            {
                query = query.IgnoreQueryFilters().Where(c => !c.IsActive);
            }

            var classes = await query
                .OrderBy(c => c.Name)
                .ToListAsync();
            return Json(new { success = true, data = classes.Select(c => new { 
                id = c.Id, 
                name = c.Name,
                ageFrom = c.AgeFrom,
                ageTo = c.AgeTo
            }) });
        }

        [HttpGet("Api/Classes/Overview")]
        public async Task<IActionResult> GetClassesOverview(bool showInactive = false)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);

            IQueryable<Class> query;
            if (showInactive)
            {
                query = _context.Classes.IgnoreQueryFilters().Where(c => !c.IsActive);
            }
            else
            {
                query = _context.Classes;
            }

            var classes = await query
                .Include(c => c.Students)
                .Include(c => c.Assignments)
                    .ThenInclude(a => a.Employee)
                .OrderBy(c => c.Name)
                .ToListAsync();

            var data = classes.Select(c => new
            {
                id = c.Id,
                name = c.Name,
                ageFrom = c.AgeFrom,
                ageTo = c.AgeTo,
                schoolYear = c.SchoolYear,
                maxCapacity = c.MaxCapacity,
                isActive = c.IsActive,
                studentCount = c.Students.Count,
                teachers = c.Assignments
                    .Where(a => a.StartDate <= today && (a.EndDate == null || a.EndDate >= today))
                    .Select(a => new
                    {
                        employeeId = a.EmployeeId,
                        teacherName = a.Employee.LastName + " " + a.Employee.FirstName,
                        roleInClass = TeacherRoleDisplay.ToDisplayName(a.RoleInClass)
                    })
                    .ToList()
            });

            return Json(new { success = true, data });
        }

        [HttpGet("Api/Class/{id:int}")]
        public async Task<IActionResult> GetClass(int id)
        {
            var classroom = await _context.Classes
                .Include(c => c.Assignments)
                .ThenInclude(a => a.Employee)
                .Include(c => c.Students)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (classroom == null)
                return Json(new { success = false, message = "Không tìm thấy lớp học." });

            var today = DateOnly.FromDateTime(DateTime.Now);

            return Json(new
            {
                success = true,
                data = new
                {
                    id = classroom.Id,
                    name = classroom.Name,
                    ageFrom = classroom.AgeFrom,
                    ageTo = classroom.AgeTo,
                    schoolYear = classroom.SchoolYear,
                    maxCapacity = classroom.MaxCapacity,
                    studentCount = classroom.Students.Count,
                    teachers = classroom.Assignments
                        .Where(a => a.StartDate <= today && (a.EndDate == null || a.EndDate >= today))
                        .Select(a => new
                        {
                            employeeId = a.EmployeeId,
                            teacherName = a.Employee.LastName + " " + a.Employee.FirstName,
                            roleInClass = TeacherRoleDisplay.ToDisplayName(a.RoleInClass)
                        })
                        .ToList()
                }
            });
        }

        [HttpPost("Api/Class")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClass([FromBody] SaveClassViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
                return Json(new { success = false, message = "Tên lớp không được để trống." });

            var trimmedName = model.Name.Trim();
            if (trimmedName.Length < 2)
                return Json(new { success = false, message = "Tên lớp phải có ít nhất 2 ký tự." });

            // Validate age range
            var validAgeRanges = new List<(int?, int?)> { (2, 3), (3, 4), (4, 5), (5, 6) };
            if (!validAgeRanges.Any(r => r.Item1 == model.AgeFrom && r.Item2 == model.AgeTo))
            {
                return Json(new { success = false, message = "Độ tuổi không hợp lệ. Vui lòng chọn trong danh sách cho phép." });
            }

            // Validate MaxCapacity
            var maxCapacity = model.MaxCapacity > 0 ? model.MaxCapacity : 25;
            if (maxCapacity < 1 || maxCapacity > 100)
                return Json(new { success = false, message = "Sĩ số tối đa phải từ 1 đến 100." });

            var schoolYearValidation = NormalizeSchoolYear(model.SchoolYear);
            if (!schoolYearValidation.IsValid)
                return Json(new { success = false, message = schoolYearValidation.ErrorMessage });
            var schoolYear = schoolYearValidation.Value!;

            var duplicate = await _context.Classes.AnyAsync(c => c.Name == trimmedName && c.SchoolYear == schoolYear);
            if (duplicate)
                return Json(new { success = false, message = "Đã tồn tại lớp cùng tên trong niên khóa này." });

            var classroom = new Class
            {
                Name = trimmedName,
                AgeFrom = model.AgeFrom,
                AgeTo = model.AgeTo,
                SchoolYear = schoolYear,
                MaxCapacity = maxCapacity,
                IsActive = true
            };

            _context.Classes.Add(classroom);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Tạo lớp học thành công." });
        }

        [HttpPut("Api/Class/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateClass(int id, [FromBody] SaveClassViewModel model)
        {
            var classroom = await _context.Classes.FindAsync(id);
            if (classroom == null)
                return Json(new { success = false, message = "Không tìm thấy lớp học." });

            if (string.IsNullOrWhiteSpace(model.Name))
                return Json(new { success = false, message = "Tên lớp không được để trống." });

            var trimmedName = model.Name.Trim();
            if (trimmedName.Length < 2)
                return Json(new { success = false, message = "Tên lớp phải có ít nhất 2 ký tự." });

            // Validate age range
            var validAgeRanges = new List<(int?, int?)> { (2, 3), (3, 4), (4, 5), (5, 6) };
            if (!validAgeRanges.Any(r => r.Item1 == model.AgeFrom && r.Item2 == model.AgeTo))
            {
                return Json(new { success = false, message = "Độ tuổi không hợp lệ. Vui lòng chọn trong danh sách cho phép." });
            }

            // Validate MaxCapacity
            var maxCapacity = model.MaxCapacity > 0 ? model.MaxCapacity : 25;
            if (maxCapacity < 1 || maxCapacity > 100)
                return Json(new { success = false, message = "Sĩ số tối đa phải từ 1 đến 100." });

            var schoolYearValidation = NormalizeSchoolYear(model.SchoolYear);
            if (!schoolYearValidation.IsValid)
                return Json(new { success = false, message = schoolYearValidation.ErrorMessage });
            var schoolYear = schoolYearValidation.Value!;

            var duplicate = await _context.Classes.AnyAsync(c =>
                c.Id != id && c.Name == trimmedName && c.SchoolYear == schoolYear);
            if (duplicate)
                return Json(new { success = false, message = "Đã tồn tại lớp cùng tên trong niên khóa này." });

            classroom.Name = trimmedName;
            classroom.AgeFrom = model.AgeFrom;
            classroom.AgeTo = model.AgeTo;
            classroom.SchoolYear = schoolYear;
            classroom.MaxCapacity = maxCapacity;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Cập nhật lớp học thành công." });
        }

        [HttpDelete("Api/Class/{id:int}")]
        public async Task<IActionResult> DeleteClass(int id)
        {
            var classroom = await _context.Classes.FindAsync(id);
            if (classroom == null)
                return Json(new { success = false, message = "Không tìm thấy lớp học." });

            classroom.IsActive = false;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã ẩn lớp học thành công." });
        }

        [HttpPost("Api/Class/Reactivate/{id:int}")]
        public async Task<IActionResult> ReactivateClass(int id)
        {
            var classroom = await _context.Classes.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);
            if (classroom == null) return Json(new { success = false, message = "Không tìm thấy lớp học." });

            classroom.IsActive = true;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã khôi phục lớp học thành công." });
        }

        private static (bool IsValid, string? Value, string ErrorMessage) NormalizeSchoolYear(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return (false, null, "Niên khóa không được để trống.");

            var schoolYear = value.Trim();
            var match = System.Text.RegularExpressions.Regex.Match(schoolYear, @"^(\d{4})-(\d{4})$");
            if (!match.Success)
                return (false, null, "Niên khóa phải có định dạng yyyy-yyyy (ví dụ: 2025-2026).");

            var startYear = int.Parse(match.Groups[1].Value);
            var endYear = int.Parse(match.Groups[2].Value);
            if (endYear != startYear + 1)
                return (false, null, "Niên khóa phải là hai năm liên tiếp (ví dụ: 2025-2026).");

            return (true, schoolYear, string.Empty);
        }
    }
}

