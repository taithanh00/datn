using datn.Data;
using datn.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace datn.Controllers
{
    [Authorize(Roles = "Manager")]
    [Route("[controller]")]
    public class ManagerController : Controller
    {
        private readonly AppDbContext _context;

        public ManagerController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Manager/Health - Debug endpoint
        [AllowAnonymous]
        [HttpGet("Health")]
        public IActionResult Health()
        {
            var user = User;
            var userClaims = User.Claims.Select(c => $"{c.Type}: {c.Value}").ToList();
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "No role";

            return Json(new
            {
                message = "Manager Controller is working",
                userName = User.Identity?.Name ?? "Not authenticated",
                role = role,
                isAuthenticated = User.Identity?.IsAuthenticated ?? false,
                claims = userClaims
            });
        }

        // GET: /Manager/Students
        [HttpGet("Students")]
        public IActionResult Students()
        {
            ViewData["Title"] = "Danh sách Học sinh";
            return View();
        }

        // GET: /Manager/Api/Students - Get all students
        [HttpGet("Api/Students")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetStudents()
        {
            try
            {
                var students = await _context.Students
                    .Include(s => s.Class)
                    .OrderBy(s => s.Id)
                    .ToListAsync();

                var result = students.Select(s => new
                {
                    id = s.Id,
                    fullName = $"{s.FirstName} {s.LastName}",
                    firstName = s.FirstName,
                    lastName = s.LastName,
                    gender = s.Gender.HasValue ? (s.Gender.Value ? "Nam" : "Nữ") : "Chưa xác định",
                    dateOfBirth = s.DateOfBirth?.ToString("dd/MM/yyyy") ?? "N/A",
                    className = s.Class?.Name ?? "Chưa có lớp",
                    enrollDate = s.EnrollDate?.ToString("dd/MM/yyyy") ?? "N/A",
                    address = s.Address ?? ""
                }).ToList();

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: /Manager/Api/Student/{id} - Get single student
        [HttpGet("Api/Student/{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetStudent(int id)
        {
            try
            {
                var student = await _context.Students
                    .Include(s => s.Class)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (student == null)
                    return Json(new { success = false, message = "Không tìm thấy học sinh" });

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        id = student.Id,
                        firstName = student.FirstName ?? "",
                        lastName = student.LastName ?? "",
                        gender = student.Gender,
                        dateOfBirth = student.DateOfBirth?.ToString("yyyy-MM-dd") ?? "",
                        address = student.Address ?? "",
                        classId = student.ClassId ?? 0,
                        enrollDate = student.EnrollDate?.ToString("yyyy-MM-dd") ?? ""
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: /Manager/Api/Classes - Get all classes for dropdown
        [HttpGet("Api/Classes")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetClasses()
        {
            try
            {
                var classes = await _context.Classes
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    data = classes.Select(c => new { id = c.Id, name = c.Name })
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /Manager/Api/Student - Create new student
        [HttpPost("Api/Student")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> CreateStudent([FromBody] CreateStudentViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ" });

                var student = new Student
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Gender = string.IsNullOrEmpty(model.Gender) ? null : model.Gender == "true",
                    DateOfBirth = string.IsNullOrEmpty(model.DateOfBirth) ? null : DateOnly.Parse(model.DateOfBirth),
                    Address = model.Address,
                    ClassId = model.ClassId > 0 ? model.ClassId : null,
                    EnrollDate = string.IsNullOrEmpty(model.EnrollDate) ? null : DateOnly.Parse(model.EnrollDate)
                };

                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Thêm học sinh thành công", studentId = student.Id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // PUT: /Manager/Api/Student/{id} - Update student
        [HttpPut("Api/Student/{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> UpdateStudent(int id, [FromBody] UpdateStudentViewModel model)
        {
            try
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == id);
                if (student == null)
                    return Json(new { success = false, message = "Không tìm thấy học sinh" });

                if (!string.IsNullOrEmpty(model.FirstName))
                    student.FirstName = model.FirstName;
                if (!string.IsNullOrEmpty(model.LastName))
                    student.LastName = model.LastName;
                if (!string.IsNullOrEmpty(model.Gender))
                    student.Gender = model.Gender == "true";
                if (!string.IsNullOrEmpty(model.DateOfBirth))
                    student.DateOfBirth = DateOnly.Parse(model.DateOfBirth);
                if (!string.IsNullOrEmpty(model.Address))
                    student.Address = model.Address;
                if (model.ClassId.HasValue && model.ClassId > 0)
                    student.ClassId = model.ClassId;
                if (!string.IsNullOrEmpty(model.EnrollDate))
                    student.EnrollDate = DateOnly.Parse(model.EnrollDate);

                _context.Students.Update(student);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật học sinh thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // DELETE: /Manager/Api/Student/{id} - Delete student
        [HttpDelete("Api/Student/{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            try
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == id);
                if (student == null)
                    return Json(new { success = false, message = "Không tìm thấy học sinh" });

                _context.Students.Remove(student);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa học sinh thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }
    }

    public class CreateStudentViewModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Gender { get; set; }
        public string DateOfBirth { get; set; }
        public string Address { get; set; }
        public int ClassId { get; set; }
        public string EnrollDate { get; set; }
    }

    public class UpdateStudentViewModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Gender { get; set; }
        public string DateOfBirth { get; set; }
        public string Address { get; set; }
        public int? ClassId { get; set; }
        public string EnrollDate { get; set; }
    }
}
