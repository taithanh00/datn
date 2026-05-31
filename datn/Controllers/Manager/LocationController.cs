using datn.Data;
using datn.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace datn.Controllers.Manager
{
    [Authorize(Roles = "Manager")]
    [Route("Manager")]
    public class LocationController : BaseController
    {
        public LocationController(AppDbContext context) : base(context) { }

        // ============ LOCATION API ============

        [HttpGet("Api/Locations")]
        public async Task<IActionResult> GetLocations(bool showInactive = false)
        {
            var query = _context.Locations.AsQueryable();
            if (showInactive)
            {
                query = query.IgnoreQueryFilters().Where(l => !l.IsActive);
            }

            var locations = await query.OrderBy(l => l.Name).ToListAsync();
            return Json(new { success = true, data = locations });
        }

        [HttpPost("Api/Location")]
        public async Task<IActionResult> CreateLocation([FromBody] Location model)
        {
            if (string.IsNullOrWhiteSpace(model.Name)) 
                return Json(new { success = false, message = "Tên địa điểm không được để trống" });
            
            var trimmedName = model.Name.Trim();
            if (trimmedName.Length < 1)
                return Json(new { success = false, message = "Tên địa điểm không được để trống" });

            // Validate Capacity
            if (model.Capacity < 1)
                return Json(new { success = false, message = "Sức chứa phải ít nhất 1 người." });
            
            if (model.Capacity > 100)
                return Json(new { success = false, message = "Sức chứa không được vượt quá 100 người." });

            model.Name = trimmedName;
            model.IsActive = true;
            _context.Locations.Add(model);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã thêm địa điểm" });
        }

        [HttpPut("Api/Location/{id:int}")]
        public async Task<IActionResult> UpdateLocation(int id, [FromBody] Location model)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location == null) 
                return Json(new { success = false, message = "Không tìm thấy." });
            
            if (string.IsNullOrWhiteSpace(model.Name))
                return Json(new { success = false, message = "Tên địa điểm không được để trống" });

            var trimmedName = model.Name.Trim();
            if (trimmedName.Length < 1)
                return Json(new { success = false, message = "Tên địa điểm không được để trống" });

            // Validate Capacity
            if (model.Capacity < 1)
                return Json(new { success = false, message = "Sức chứa phải ít nhất 1 người." });
            
            if (model.Capacity > 100)
                return Json(new { success = false, message = "Sức chứa không được vượt quá 100 người." });

            location.Name = trimmedName;
            location.Capacity = model.Capacity;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã cập nhật địa điểm" });
        }

        [HttpDelete("Api/Location/{id:int}")]
        public async Task<IActionResult> DeleteLocation(int id)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location == null) return Json(new { success = false, message = "Không tìm thấy." });

            location.IsActive = false;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã ẩn địa điểm thành công." });
        }

        [HttpPost("Api/Location/Reactivate/{id:int}")]
        public async Task<IActionResult> ReactivateLocation(int id)
        {
            var location = await _context.Locations.IgnoreQueryFilters().FirstOrDefaultAsync(l => l.Id == id);
            if (location == null) return Json(new { success = false, message = "Không tìm thấy." });

            location.IsActive = true;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã khôi phục địa điểm thành công." });
        }
    }
}
