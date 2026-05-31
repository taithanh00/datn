using datn.Data;
using datn.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace datn.Controllers.Manager
{
    [Authorize(Roles = "Manager")]
    [Route("Manager")]
    public class ActivityController : BaseController
    {
        public ActivityController(AppDbContext context) : base(context) { }

        // ============ ACTIVITY API ============

        [HttpGet("Activities")]
        public IActionResult Activities()
        {
            return View("~/Views/Dashboard/Admin/Manager/Activities.cshtml");
        }

        [HttpGet("Api/Activities")]
        public async Task<IActionResult> GetActivities(bool showInactive = false)
        {
            var query = _context.Activities
                .Include(a => a.Location)
                .Include(a => a.Organizer)
                .Include(a => a.ClassActivities)
                    .ThenInclude(ca => ca.Class)
                .AsQueryable();

            if (showInactive)
            {
                query = query.IgnoreQueryFilters().Where(a => !a.IsActive);
            }

            var activities = await query
                .OrderByDescending(a => a.Date)
                .ToListAsync();

            var data = activities.Select(a => new
            {
                id = a.Id,
                name = a.Name,
                description = a.Description,
                date = a.Date?.ToString("yyyy-MM-dd"),
                locationId = a.LocationId,
                locationName = a.Location?.Name,
                organizerId = a.OrganizerId,
                organizerName = a.Organizer?.FullName,
                isActive = a.IsActive,
                classes = a.ClassActivities.Select(ca => new { id = ca.ClassId, name = ca.Class.Name })
            });

            return Json(new { success = true, data });
        }

        [HttpPost("Api/Activity")]
        public async Task<IActionResult> CreateActivity([FromBody] SaveActivityViewModel model)
        {
            // Validate Name
            if (string.IsNullOrWhiteSpace(model.Name))
                return Json(new { success = false, message = "Tên hoạt động không được để trống." });
            
            var trimmedName = model.Name.Trim();
            if (trimmedName.Length < 2)
                return Json(new { success = false, message = "Tên hoạt động phải có ít nhất 2 ký tự." });

            // Validate Date
            if (string.IsNullOrWhiteSpace(model.Date))
                return Json(new { success = false, message = "Ngày hoạt động không được để trống." });
            
            if (!DateOnly.TryParse(model.Date, out var activityDate))
                return Json(new { success = false, message = "Ngày hoạt động không hợp lệ." });

            // Validate LocationId if provided
            if (model.LocationId.HasValue && model.LocationId > 0)
            {
                var locationExists = await _context.Locations.AnyAsync(l => l.Id == model.LocationId);
                if (!locationExists)
                    return Json(new { success = false, message = "Không tìm thấy địa điểm này." });
            }

            // Validate OrganizerId if provided
            if (model.OrganizerId.HasValue && model.OrganizerId > 0)
            {
                var organizerExists = await _context.Employees.AnyAsync(e => e.Id == model.OrganizerId);
                if (!organizerExists)
                    return Json(new { success = false, message = "Không tìm thấy người tổ chức." });
            }

            // Validate ClassIds if provided
            if (model.ClassIds != null && model.ClassIds.Any())
            {
                var invalidClassIds = model.ClassIds.Where(cid => cid <= 0).ToList();
                if (invalidClassIds.Any())
                    return Json(new { success = false, message = "Lớp học ID không hợp lệ." });
                
                var existingClassIds = await _context.Classes
                    .Where(c => model.ClassIds.Contains(c.Id))
                    .Select(c => c.Id)
                    .ToListAsync();
                
                if (existingClassIds.Count != model.ClassIds.Count)
                    return Json(new { success = false, message = "Một số lớp học không tồn tại." });
            }

            var activity = new Activity
            {
                Name = trimmedName,
                Description = model.Description?.Trim(),
                Date = activityDate,
                LocationId = model.LocationId,
                OrganizerId = model.OrganizerId,
                IsActive = true
            };

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            if (model.ClassIds != null && model.ClassIds.Any())
            {
                foreach (var classId in model.ClassIds)
                {
                    _context.ClassActivities.Add(new ClassActivity { ActivityId = activity.Id, ClassId = classId });
                }
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Đã tạo hoạt động thành công" });
        }

        [HttpPut("Api/Activity/{id:int}")]
        public async Task<IActionResult> UpdateActivity(int id, [FromBody] SaveActivityViewModel model)
        {
            var activity = await _context.Activities
                .Include(a => a.ClassActivities)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (activity == null) 
                return Json(new { success = false, message = "Không tìm thấy hoạt động" });

            // Validate Name
            if (string.IsNullOrWhiteSpace(model.Name))
                return Json(new { success = false, message = "Tên hoạt động không được để trống." });
            
            var trimmedName = model.Name.Trim();
            if (trimmedName.Length < 2)
                return Json(new { success = false, message = "Tên hoạt động phải có ít nhất 2 ký tự." });

            // Validate Date
            if (string.IsNullOrWhiteSpace(model.Date))
                return Json(new { success = false, message = "Ngày hoạt động không được để trống." });
            
            if (!DateOnly.TryParse(model.Date, out var activityDate))
                return Json(new { success = false, message = "Ngày hoạt động không hợp lệ." });

            // Validate LocationId if provided
            if (model.LocationId.HasValue && model.LocationId > 0)
            {
                var locationExists = await _context.Locations.AnyAsync(l => l.Id == model.LocationId);
                if (!locationExists)
                    return Json(new { success = false, message = "Không tìm thấy địa điểm này." });
            }

            // Validate OrganizerId if provided
            if (model.OrganizerId.HasValue && model.OrganizerId > 0)
            {
                var organizerExists = await _context.Employees.AnyAsync(e => e.Id == model.OrganizerId);
                if (!organizerExists)
                    return Json(new { success = false, message = "Không tìm thấy người tổ chức." });
            }

            // Validate ClassIds if provided
            if (model.ClassIds != null && model.ClassIds.Any())
            {
                var invalidClassIds = model.ClassIds.Where(cid => cid <= 0).ToList();
                if (invalidClassIds.Any())
                    return Json(new { success = false, message = "Lớp học ID không hợp lệ." });
                
                var existingClassIds = await _context.Classes
                    .Where(c => model.ClassIds.Contains(c.Id))
                    .Select(c => c.Id)
                    .ToListAsync();
                
                if (existingClassIds.Count != model.ClassIds.Count)
                    return Json(new { success = false, message = "Một số lớp học không tồn tại." });
            }

            activity.Name = trimmedName;
            activity.Description = model.Description?.Trim();
            activity.Date = activityDate;
            activity.LocationId = model.LocationId;
            activity.OrganizerId = model.OrganizerId;

            // Update ClassActivities
            _context.ClassActivities.RemoveRange(activity.ClassActivities);
            if (model.ClassIds != null && model.ClassIds.Any())
            {
                foreach (var classId in model.ClassIds)
                {
                    _context.ClassActivities.Add(new ClassActivity { ActivityId = id, ClassId = classId });
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã cập nhật hoạt động" });
        }

        [HttpDelete("Api/Activity/{id:int}")]
        public async Task<IActionResult> DeleteActivity(int id)
        {
            var activity = await _context.Activities.FindAsync(id);
            if (activity == null) return Json(new { success = false, message = "Không tìm thấy hoạt động" });

            activity.IsActive = false;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã ẩn hoạt động thành công." });
        }

        [HttpPost("Api/Activity/Reactivate/{id:int}")]
        public async Task<IActionResult> ReactivateActivity(int id)
        {
            var activity = await _context.Activities.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == id);
            if (activity == null) return Json(new { success = false, message = "Không tìm thấy." });

            activity.IsActive = true;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã khôi phục hoạt động thành công." });
        }
    }
}
