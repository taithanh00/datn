using datn.Data;
using datn.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace datn.Controllers.Manager
{
    [Authorize(Roles = "Manager")]
    [Route("Manager")]
    public class CurriculumController : BaseController
    {
        public CurriculumController(AppDbContext context) : base(context) { }

        // ============ CURRICULUM API ============

        [HttpGet("Curriculums")]
        public IActionResult Curriculums()
        {
            return View("~/Views/Dashboard/Admin/Manager/Curriculums.cshtml");
        }

        [HttpGet("Api/Curriculums")]
        public async Task<IActionResult> GetCurriculums(bool showInactive = false)
        {
            var query = _context.Curriculums.Include(c => c.Subject).AsQueryable();
            if (!showInactive) query = query.Where(c => c.IsActive);
            var curriculums = await query.OrderBy(c => c.Title).ToListAsync();

            var data = curriculums.Select(c => new
            {
                id = c.Id,
                title = c.Title,
                description = c.Description,
                content = c.Content,
                subjectId = c.SubjectId,
                subjectName = c.Subject?.Name,
                ageFrom = c.AgeFrom,
                ageTo = c.AgeTo,
                isActive = c.IsActive
            });

            return Json(new { success = true, data });
        }

        [HttpPost("Api/Curriculum")]
        public async Task<IActionResult> CreateCurriculum([FromBody] Curriculum model)
        {
            // Validate Title
            if (string.IsNullOrWhiteSpace(model.Title))
                return Json(new { success = false, message = "Tiêu đề chương trình học không được để trống." });
            
            var trimmedTitle = model.Title.Trim();
            if (trimmedTitle.Length < 2)
                return Json(new { success = false, message = "Tiêu đề phải có ít nhất 2 ký tự." });

            // Validate SubjectId
            if (model.SubjectId <= 0)
                return Json(new { success = false, message = "Vui lòng chọn môn học." });
            
            var subjectExists = await _context.Subjects.AnyAsync(s => s.Id == model.SubjectId);
            if (!subjectExists)
                return Json(new { success = false, message = "Môn học không tồn tại." });

            // Validate AgeFrom, AgeTo
            if (model.AgeFrom <= 0 || model.AgeTo <= 0)
                return Json(new { success = false, message = "Độ tuổi bắt đầu và kết thúc phải lớn hơn 0." });
            
            if (model.AgeFrom >= model.AgeTo)
                return Json(new { success = false, message = "Độ tuổi bắt đầu phải nhỏ hơn độ tuổi kết thúc." });
            
            if (model.AgeFrom < 2 || model.AgeTo > 6)
                return Json(new { success = false, message = "Độ tuổi phải nằm trong khoảng 2-6 tuổi." });

            model.Title = trimmedTitle;
            model.Description = model.Description?.Trim();
            model.Content = model.Content?.Trim();
            model.IsActive = true;
            _context.Curriculums.Add(model);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã tạo chương trình học" });
        }

        [HttpPut("Api/Curriculum/{id:int}")]
        public async Task<IActionResult> UpdateCurriculum(int id, [FromBody] Curriculum model)
        {
            var cur = await _context.Curriculums.FindAsync(id);
            if (cur == null) 
                return Json(new { success = false, message = "Không tìm thấy" });

            // Validate Title
            if (string.IsNullOrWhiteSpace(model.Title))
                return Json(new { success = false, message = "Tiêu đề chương trình học không được để trống." });
            
            var trimmedTitle = model.Title.Trim();
            if (trimmedTitle.Length < 2)
                return Json(new { success = false, message = "Tiêu đề phải có ít nhất 2 ký tự." });

            // Validate SubjectId
            if (model.SubjectId <= 0)
                return Json(new { success = false, message = "Vui lòng chọn môn học." });
            
            var subjectExists = await _context.Subjects.AnyAsync(s => s.Id == model.SubjectId);
            if (!subjectExists)
                return Json(new { success = false, message = "Môn học không tồn tại." });

            // Validate AgeFrom, AgeTo
            if (model.AgeFrom <= 0 || model.AgeTo <= 0)
                return Json(new { success = false, message = "Độ tuổi bắt đầu và kết thúc phải lớn hơn 0." });
            
            if (model.AgeFrom >= model.AgeTo)
                return Json(new { success = false, message = "Độ tuổi bắt đầu phải nhỏ hơn độ tuổi kết thúc." });
            
            if (model.AgeFrom < 2 || model.AgeTo > 6)
                return Json(new { success = false, message = "Độ tuổi phải nằm trong khoảng 2-6 tuổi." });

            cur.Title = trimmedTitle;
            cur.Description = model.Description?.Trim();
            cur.Content = model.Content?.Trim();
            cur.SubjectId = model.SubjectId;
            cur.AgeFrom = model.AgeFrom;
            cur.AgeTo = model.AgeTo;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã cập nhật" });
        }

        [HttpDelete("Api/Curriculum/{id:int}")]
        public async Task<IActionResult> DeleteCurriculum(int id)
        {
            var cur = await _context.Curriculums.FindAsync(id);
            if (cur == null) return Json(new { success = false, message = "Không tìm thấy." });

            cur.IsActive = false;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã ẩn chương trình học thành công." });
        }

        [HttpPost("Api/Curriculum/Reactivate/{id:int}")]
        public async Task<IActionResult> ReactivateCurriculum(int id)
        {
            var cur = await _context.Curriculums.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);
            if (cur == null) return Json(new { success = false, message = "Không tìm thấy." });

            cur.IsActive = true;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã khôi phục chương trình học thành công." });
        }
    }
}
