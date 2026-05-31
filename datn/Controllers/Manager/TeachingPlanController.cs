using datn.Data;
using datn.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace datn.Controllers.Manager
{
    [Authorize(Roles = "Manager")]
    [Route("Manager")]
    public class TeachingPlanController : BaseController
    {
        public TeachingPlanController(AppDbContext context) : base(context) { }

        // ============ TEACHING PLAN API ============

        [HttpGet("TeachingPlans")]
        public IActionResult TeachingPlans()
        {
            return View("~/Views/Dashboard/Admin/Manager/TeachingPlans.cshtml");
        }

        [HttpGet("Api/TeachingPlans")]
        public async Task<IActionResult> GetTeachingPlans(int? classId, bool showInactive = false)
        {
            var query = _context.TeachingPlans
                .Include(tp => tp.Class)
                .Include(tp => tp.Curriculum)
                .AsQueryable();

            if (classId.HasValue) query = query.Where(tp => tp.ClassId == classId);
            if (!showInactive) query = query.Where(tp => tp.IsActive);

            var plans = await query.OrderByDescending(tp => tp.StartDate).ToListAsync();
            var data = plans.Select(tp => new
            {
                classId = tp.ClassId,
                className = tp.Class.Name,
                curriculumId = tp.CurriculumId,
                curriculumTitle = tp.Curriculum.Title,
                startDate = tp.StartDate.ToString("yyyy-MM-dd"),
                endDate = tp.EndDate?.ToString("yyyy-MM-dd"),
                status = tp.Status,
                isActive = tp.IsActive
            });

            return Json(new { success = true, data });
        }

        [HttpPost("Api/TeachingPlan")]
        public async Task<IActionResult> CreateTeachingPlan([FromBody] TeachingPlan model)
        {
            // Validate ClassId
            if (model.ClassId <= 0)
                return Json(new { success = false, message = "Vui lòng chọn lớp học." });
            
            var classExists = await _context.Classes.AnyAsync(c => c.Id == model.ClassId);
            if (!classExists)
                return Json(new { success = false, message = "Lớp học không tồn tại." });

            // Validate CurriculumId
            if (model.CurriculumId <= 0)
                return Json(new { success = false, message = "Vui lòng chọn chương trình học." });
            
            var curriculumExists = await _context.Curriculums.AnyAsync(c => c.Id == model.CurriculumId);
            if (!curriculumExists)
                return Json(new { success = false, message = "Chương trình học không tồn tại." });

            // Validate StartDate
            if (model.StartDate == DateOnly.MinValue)
                return Json(new { success = false, message = "Ngày bắt đầu không hợp lệ." });

            // Validate EndDate if provided
            if (model.EndDate.HasValue && model.EndDate != DateOnly.MinValue)
            {
                if (model.EndDate < model.StartDate)
                    return Json(new { success = false, message = "Ngày kết thúc phải sau ngày bắt đầu." });
            }

            if (await _context.TeachingPlans.AnyAsync(tp => tp.ClassId == model.ClassId && tp.CurriculumId == model.CurriculumId && tp.StartDate == model.StartDate))
                return Json(new { success = false, message = "Kế hoạch này đã tồn tại" });
            
            model.IsActive = true;
            _context.TeachingPlans.Add(model);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã lập kế hoạch giảng dạy" });
        }

        [HttpPut("Api/TeachingPlan")]
        public async Task<IActionResult> UpdateTeachingPlan([FromBody] TeachingPlan model)
        {
            var plan = await _context.TeachingPlans.FirstOrDefaultAsync(tp => 
                tp.ClassId == model.ClassId && 
                tp.CurriculumId == model.CurriculumId && 
                tp.StartDate == model.StartDate);

            if (plan == null) 
                return Json(new { success = false, message = "Không tìm thấy kế hoạch để cập nhật" });

            // Validate EndDate if provided
            if (model.EndDate.HasValue && model.EndDate != DateOnly.MinValue)
            {
                if (model.EndDate < model.StartDate)
                    return Json(new { success = false, message = "Ngày kết thúc phải sau ngày bắt đầu." });
            }

            plan.EndDate = model.EndDate;
            plan.Status = model.Status;

            _context.TeachingPlans.Update(plan);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật kế hoạch thành công" });
        }

        [HttpDelete("Api/TeachingPlan")]
        public async Task<IActionResult> DeleteTeachingPlan(int classId, int curriculumId, string startDate)
        {
            var sDate = DateOnly.Parse(startDate);
            var plan = await _context.TeachingPlans.FirstOrDefaultAsync(tp => tp.ClassId == classId && tp.CurriculumId == curriculumId && tp.StartDate == sDate);
            if (plan == null) return Json(new { success = false, message = "Không tìm thấy" });

            plan.IsActive = false;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã ẩn kế hoạch giảng dạy thành công." });
        }

        [HttpPost("Api/TeachingPlan/Reactivate")]
        public async Task<IActionResult> ReactivateTeachingPlan(int classId, int curriculumId, string startDate)
        {
            var sDate = DateOnly.Parse(startDate);
            var plan = await _context.TeachingPlans.IgnoreQueryFilters()
                .FirstOrDefaultAsync(tp => tp.ClassId == classId && tp.CurriculumId == curriculumId && tp.StartDate == sDate);

            if (plan == null) return Json(new { success = false, message = "Không tìm thấy." });

            plan.IsActive = true;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã khôi phục kế hoạch giảng dạy thành công." });
        }
    }
}
