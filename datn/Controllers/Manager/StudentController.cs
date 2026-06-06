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
    public class StudentController : BaseController
    {
        private readonly IStudentService _studentService;

        public StudentController(AppDbContext context, IStudentService studentService) : base(context)
        {
            _studentService = studentService;
        }

        // ============ STUDENT API ============

        [HttpGet("Api/Students")]
        public async Task<IActionResult> GetStudents(bool showInactive = false)
        {
            try
            {
                var query = _context.Students.AsQueryable();
                if (showInactive)
                {
                    query = query.IgnoreQueryFilters().Where(s => s.Status == StudentStatus.Inactive);
                }

                var students = await query
                    .Include(s => s.Class)
                    .Include(s => s.ParentStudents).ThenInclude(ps => ps.Parent)
                    .OrderBy(s => s.Id)
                    .ToListAsync();

                var result = students.Select(s => new
                {
                    id = s.Id,
                    fullName = s.FullName,
                    gender = s.Gender ? "Nam" : "Nữ",
                    dateOfBirth = s.DateOfBirth.ToString("dd/MM/yyyy"),
                    address = s.Address ?? "N/A",
                    className = s.Class?.Name ?? "Chưa có lớp",
                    enrollDate = s.EnrollDate?.ToString("dd/MM/yyyy") ?? "N/A",
                    status = (int)s.Status,
                    statusText = s.Status == StudentStatus.Active ? "Đang học" : "Đã nghỉ",
                    createdAt = s.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                    avatarPath = s.AvatarPath ?? "/images/lion_orange.png",
                    fatherName = s.ParentStudents
                        .Where(ps => ps.Relationship == "Bố")
                        .Select(ps => ps.Parent.LastName + " " + ps.Parent.FirstName)
                        .FirstOrDefault(),
                    motherName = s.ParentStudents
                        .Where(ps => ps.Relationship == "Mẹ")
                        .Select(ps => ps.Parent.LastName + " " + ps.Parent.FirstName)
                        .FirstOrDefault()
                }).ToList();

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("Api/Student/{id:int}")]
        public async Task<IActionResult> GetStudent(int id)
        {
            try
            {
                var s = await _context.Students.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == id);
                if (s == null) return Json(new { success = false, message = "Không tìm thấy" });

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        id = s.Id,
                        firstName = s.FirstName,
                        lastName = s.LastName,
                        gender = s.Gender.ToString().ToLower(),
                        dateOfBirth = s.DateOfBirth.ToString("yyyy-MM-dd"),
                        address = s.Address,
                        classId = s.ClassId,
                        enrollDate = s.EnrollDate?.ToString("yyyy-MM-dd"),
                        status = (int)s.Status,
                        avatarPath = s.AvatarPath
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("Api/Student/{id:int}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            try
            {
                var student = await _context.Students.FindAsync(id);
                if (student == null) return Json(new { success = false, message = "Không tìm thấy" });

                student.Status = StudentStatus.Inactive;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã chuyển trạng thái học sinh sang 'Đã nghỉ'" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("Api/Student/Reactivate/{id:int}")]
        public async Task<IActionResult> ReactivateStudent(int id)
        {
            try
            {
                var student = await _context.Students.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == id);
                if (student == null) return Json(new { success = false, message = "Không tìm thấy" });

                student.Status = StudentStatus.Active;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã khôi phục học sinh thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("Api/Student")]
        public async Task<IActionResult> CreateStudent([FromForm] datn.DTOs.CreateStudentDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Dữ liệu nhập vào không hợp lệ" });
                }

                // 1. Kiểm tra trùng lặp nếu không phải ép buộc tạo mới
                if (!model.ForceCreate)
                {
                    var duplicate = await _studentService.CheckPotentialDuplicateAsync(model);
                    if (duplicate != null)
                    {
                        return StatusCode(409, new { 
                            success = false, 
                            message = $"Có một học sinh tên là {duplicate.FullName}, ngày sinh {duplicate.DateOfBirth:dd/MM/yyyy} đã tồn tại trong hệ thống. Bạn có chắc muốn tạo mới không?",
                            existingStudentId = duplicate.Id
                        });
                    }
                }

                // 2. Tạo mới học sinh thông qua Service
                // Kiểm tra độ tuổi hợp lệ chung (2-6 tuổi)
                if (DateOnly.TryParse(model.DateOfBirth, out var dobStudent))
                {
                    var ageStudent = DateTime.Now.Year - dobStudent.Year;
                    if (ageStudent < 2 || ageStudent > 6)
                    {
                        return Json(new { success = false, message = $"Độ tuổi học sinh ({ageStudent} tuổi) không phù hợp để nhập học. Hệ thống chỉ nhận học sinh từ 2 đến 6 tuổi." });
                    }
                }

                // Kiểm tra các ràng buộc Lớp học (Tuổi, Sĩ số, Niên khóa)
                if (model.ClassId > 0)
                {
                    var validationError = await ValidateStudentClassAssignmentAsync(model.ClassId.Value, model.DateOfBirth, null);
                    if (validationError != null) return Json(new { success = false, message = validationError });
                }

                var student = await _studentService.CreateStudentAsync(model);
                
                return Json(new { success = true, message = "Thêm học sinh thành công", studentId = student.Id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpPut("Api/Student/{id:int}")]
        public async Task<IActionResult> UpdateStudent(int id, [FromForm] UpdateStudentViewModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.FirstName) || string.IsNullOrWhiteSpace(model.LastName))
                    return Json(new { success = false, message = "Họ và tên không được để trống." });

                if (!Enum.IsDefined(typeof(StudentStatus), model.Status))
                    return Json(new { success = false, message = "Trạng thái học sinh không hợp lệ." });

                var student = await _context.Students.FindAsync(id);
                if (student == null) return Json(new { success = false, message = "Không tìm thấy" });

                DateOnly dob;
                if (string.IsNullOrWhiteSpace(model.DateOfBirth))
                {
                    dob = student.DateOfBirth;
                }
                else if (!DateOnly.TryParse(model.DateOfBirth, out dob))
                {
                    return Json(new { success = false, message = "Ngày sinh không hợp lệ." });
                }

                var today = DateOnly.FromDateTime(DateTime.Now);
                var ageStudentUpdate = today.Year - dob.Year - ((today < dob.AddYears(today.Year - dob.Year)) ? 1 : 0);
                if (ageStudentUpdate < 2 || ageStudentUpdate > 6)
                {
                    return Json(new { success = false, message = $"Độ tuổi học sinh ({ageStudentUpdate} tuổi) không phù hợp. Hệ thống chỉ nhận học sinh từ 2 đến 6 tuổi." });
                }

                if (string.IsNullOrWhiteSpace(model.FirstName.Trim()) || string.IsNullOrWhiteSpace(model.LastName.Trim()))
                    return Json(new { success = false, message = "Họ và tên không được để trống." });

                var isDuplicate = await _context.Students.AnyAsync(s => 
                    s.Id != id &&
                    s.FirstName == model.FirstName.Trim() && 
                    s.LastName == model.LastName.Trim() && 
                    s.DateOfBirth == dob);

                if (isDuplicate)
                {
                    return Json(new { success = false, message = "Thông tin cập nhật trùng với một học sinh khác đã tồn tại." });
                }

                if (model.ClassId.HasValue && model.ClassId > 0)
                {
                    if (!await _context.Classes.AnyAsync(c => c.Id == model.ClassId.Value))
                        return Json(new { success = false, message = "Lớp học không tồn tại." });
                }

                if (model.ClassId > 0 && model.ClassId != student.ClassId)
                {
                    var validationError = await ValidateStudentClassAssignmentAsync(model.ClassId.Value, dob.ToString("yyyy-MM-dd"), id);
                    if (validationError != null) return Json(new { success = false, message = validationError });
                }

                DateOnly? enrollDate;
                if (!string.IsNullOrWhiteSpace(model.EnrollDate))
                {
                    if (!DateOnly.TryParse(model.EnrollDate, out var parsedEnrollDate))
                        return Json(new { success = false, message = "Ngày nhập học không hợp lệ." });
                    enrollDate = parsedEnrollDate;
                }
                else
                {
                    enrollDate = student.EnrollDate;
                }

                student.FirstName = model.FirstName.Trim();
                student.LastName = model.LastName.Trim();
                student.Gender = model.Gender == "true";
                student.DateOfBirth = dob;
                student.Address = model.Address?.Trim();
                student.ClassId = model.ClassId > 0 ? model.ClassId : null;
                student.Status = (StudentStatus)model.Status;
                student.EnrollDate = enrollDate;

                if (model.Avatar != null) student.AvatarPath = await SaveAvatar(model.Avatar, "student");

                _context.Students.Update(student);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cập nhật thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("Api/Students/Search")]
        public async Task<IActionResult> SearchStudents(string q)
        {
            q = q?.Trim() ?? string.Empty;
            var hasNumericId = int.TryParse(q, out var studentId);

            var students = await _context.Students
                .Where(s => s.Status == StudentStatus.Active &&
                    (s.FirstName.Contains(q) || s.LastName.Contains(q) || (hasNumericId && s.Id == studentId)))
                .Take(10)
                .Select(s => new { id = s.Id, fullName = s.LastName + " " + s.FirstName })
                .ToListAsync();
            return Json(new { success = true, data = students });
        }

        // ============ PRIVATE HELPERS ============

        private async Task<string?> ValidateStudentClassAssignmentAsync(int classId, string dobStr, int? studentId)
        {
            var classroom = await _context.Classes.Include(c => c.Students).FirstOrDefaultAsync(c => c.Id == classId);
            if (classroom == null) return "Không tìm thấy lớp học.";

            // 1. Kiểm tra Sĩ số
            var currentCount = classroom.Students.Count(s => s.Status == StudentStatus.Active);
            if (currentCount >= classroom.MaxCapacity && studentId == null) 
                return $"Lớp {classroom.Name} đã đủ sĩ số ({classroom.MaxCapacity}).";

            // 2. Kiểm tra Độ tuổi
            if (DateOnly.TryParse(dobStr, out var dob))
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                var age = CalculateAgeInYears(dob, today);
                var classAgeLabel = FormatClassAgeRange(classroom.AgeFrom, classroom.AgeTo);

                if (classroom.AgeFrom == 2 && classroom.AgeTo == 3)
                {
                    var ageInMonths = CalculateAgeInMonths(dob, today);
                    if (ageInMonths < 24)
                        return $"Học sinh ({ageInMonths} tháng) nhỏ hơn độ tuổi quy định của lớp ({classAgeLabel}).";

                    if (ageInMonths > 36)
                        return $"Học sinh ({ageInMonths} tháng) lớn hơn độ tuổi quy định của lớp ({classAgeLabel}).";
                }
                else if (classroom.AgeFrom.HasValue && age < classroom.AgeFrom.Value)
                    return $"Học sinh ({age} tuổi) nhỏ hơn độ tuổi quy định của lớp ({classAgeLabel}).";
                 
                else if (classroom.AgeTo.HasValue && age > classroom.AgeTo.Value)
                    return $"Học sinh ({age} tuổi) lớn hơn độ tuổi quy định của lớp ({classAgeLabel}).";
            }

            if (!IsCurrentOrNextSchoolYear(classroom.SchoolYear) && studentId == null)
            {
                return $"Không thể thêm học sinh vào niên khóa cũ ({classroom.SchoolYear}).";
            }

            return null;
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

        private static int CalculateAgeInYears(DateOnly dob, DateOnly today)
        {
            var age = today.Year - dob.Year;
            if (today < dob.AddYears(age)) age--;
            return age;
        }

        private static int CalculateAgeInMonths(DateOnly dob, DateOnly today)
        {
            var months = (today.Year - dob.Year) * 12 + today.Month - dob.Month;
            if (today.Day < dob.Day) months--;
            return months;
        }

        private static string FormatClassAgeRange(int? ageFrom, int? ageTo)
        {
            if (ageFrom == 2 && ageTo == 3) return "24 - 36 tháng";
            if (ageFrom.HasValue && ageTo.HasValue) return $"{ageFrom.Value} - {ageTo.Value} tuổi";
            if (ageFrom.HasValue) return $"từ {ageFrom.Value} tuổi";
            if (ageTo.HasValue) return $"đến {ageTo.Value} tuổi";
            return "chưa cập nhật";
        }

        private static bool IsCurrentOrNextSchoolYear(string? schoolYear)
        {
            if (string.IsNullOrWhiteSpace(schoolYear))
                return false;

            var match = System.Text.RegularExpressions.Regex.Match(schoolYear.Trim(), @"^(\d{4})-(\d{4})$");
            if (!match.Success)
                return false;

            var startYear = int.Parse(match.Groups[1].Value);
            var endYear = int.Parse(match.Groups[2].Value);
            if (endYear != startYear + 1)
                return false;

            var currentYear = DateTime.Now.Year;
            return startYear == currentYear || startYear == currentYear + 1;
        }
    }
}
