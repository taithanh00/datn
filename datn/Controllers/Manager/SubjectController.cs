using datn.Data;
using datn.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace datn.Controllers.Manager
{
    [Authorize(Roles = "Manager")]
    [Route("Manager")]
    public class SubjectController : BaseController
    {
        public SubjectController(AppDbContext context) : base(context) { }

        // ============ SUBJECT API ============

        [HttpGet("Api/Subjects")]
        public async Task<IActionResult> GetSubjects(bool showInactive = false)
        {
            IQueryable<Subject> query;
            if (showInactive)
            {
                query = _context.Subjects.IgnoreQueryFilters().Where(s => !s.IsActive);
            }
            else
            {
                query = _context.Subjects;
            }

            var subjects = await query
                .OrderBy(s => s.Name)
                .ToListAsync();

            return Json(new
            {
                success = true,
                data = subjects.Select(s => new
                {
                    id = s.Id,
                    name = s.Name,
                    code = s.Code,
                    description = s.Description,
                    isActive = s.IsActive
                })
            });
        }

        [HttpGet("Api/Subject/{id:int}")]
        public async Task<IActionResult> GetSubject(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
                return Json(new { success = false, message = "Không tìm thấy môn học." });

            return Json(new
            {
                success = true,
                data = new
                {
                    id = subject.Id,
                    name = subject.Name,
                    code = subject.Code,
                    description = subject.Description,
                    isActive = subject.IsActive
                }
            });
        }

        [HttpPost("Api/Subject")]
        public async Task<IActionResult> CreateSubject([FromBody] SaveSubjectViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Code))
                return Json(new { success = false, message = "Tên môn và mã môn là bắt buộc." });

            var trimmedName = model.Name.Trim();
            if (trimmedName.Length < 2)
                return Json(new { success = false, message = "Tên môn phải có ít nhất 2 ký tự." });

            var normalizedCode = model.Code.Trim().ToUpperInvariant();
            
            // Validate Code pattern: 2-10 characters, letters/numbers/hyphens only
            if (!System.Text.RegularExpressions.Regex.IsMatch(normalizedCode, @"^[A-Z0-9\-]{2,10}$"))
                return Json(new { success = false, message = "Mã môn phải từ 2-10 ký tự, chỉ chứa chữ cái, số hoặc dấu gạch ngang." });

            var duplicate = await _context.Subjects.AnyAsync(s => s.Code == normalizedCode);
            if (duplicate)
                return Json(new { success = false, message = "Mã môn đã tồn tại." });

            _context.Subjects.Add(new Subject
            {
                Name = trimmedName,
                Code = normalizedCode,
                Description = model.Description?.Trim(),
                IsActive = model.IsActive
            });

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Tạo môn học thành công." });
        }

        [HttpPut("Api/Subject/{id:int}")]
        public async Task<IActionResult> UpdateSubject(int id, [FromBody] SaveSubjectViewModel model)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
                return Json(new { success = false, message = "Không tìm thấy môn học." });

            if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Code))
                return Json(new { success = false, message = "Tên môn và mã môn là bắt buộc." });

            var trimmedName = model.Name.Trim();
            if (trimmedName.Length < 2)
                return Json(new { success = false, message = "Tên môn phải có ít nhất 2 ký tự." });

            var normalizedCode = model.Code.Trim().ToUpperInvariant();
            
            // Validate Code pattern: 2-10 characters, letters/numbers/hyphens only
            if (!System.Text.RegularExpressions.Regex.IsMatch(normalizedCode, @"^[A-Z0-9\-]{2,10}$"))
                return Json(new { success = false, message = "Mã môn phải từ 2-10 ký tự, chỉ chứa chữ cái, số hoặc dấu gạch ngang." });

            var duplicate = await _context.Subjects.AnyAsync(s => s.Id != id && s.Code == normalizedCode);
            if (duplicate)
                return Json(new { success = false, message = "Mã môn đã tồn tại." });

            subject.Name = trimmedName;
            subject.Code = normalizedCode;
            subject.Description = model.Description?.Trim();
            subject.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật môn học thành công." });
        }

        [HttpDelete("Api/Subject/{id:int}")]
        public async Task<IActionResult> DeleteSubject(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null) return Json(new { success = false, message = "Không tìm thấy môn học." });

            subject.IsActive = false;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã ẩn môn học thành công." });
        }

        [HttpPost("Api/Subject/Reactivate/{id:int}")]
        public async Task<IActionResult> ReactivateSubject(int id)
        {
            var subject = await _context.Subjects.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == id);
            if (subject == null) return Json(new { success = false, message = "Không tìm thấy." });

            subject.IsActive = true;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã khôi phục môn học thành công." });
        }
    }
}
