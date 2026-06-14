using datn.Data;
using datn.Models;
using datn.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace datn.Controllers.Manager
{
    [Authorize(Roles = "Manager")]
    [Route("[controller]")]
    public class HolidayManagementController : BaseController
    {
        private readonly INotificationService _notificationService;

        public HolidayManagementController(AppDbContext context, INotificationService notificationService) : base(context) 
        { 
            _notificationService = notificationService;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Quản lý ngày lễ";
            return View("~/Views/Dashboard/Admin/HolidayManagement/Index.cshtml");
        }

        [HttpGet("Api/List")]
        public async Task<IActionResult> List(bool showInactive = false)
        {
            var query = _context.Holidays.AsQueryable();
            if (showInactive)
            {
                query = query.IgnoreQueryFilters().Where(h => !h.IsActive);
            }

            var holidays = await query
                .OrderByDescending(h => h.Date)
                .ToListAsync();
            return Json(new { success = true, data = holidays });
        }

        [HttpPost("Api/Create")]
        public async Task<IActionResult> Create([FromBody] Holiday model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
                return Json(new { success = false, message = "Vui lòng nhập tên ngày lễ." });

            // Sử dụng IgnoreQueryFilters để kiểm tra trùng ngày kể cả với ngày đã ẩn
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (model.Date < today)
                return Json(new { success = false, message = "Ch\u1ec9 \u0111\u01b0\u1ee3c t\u1ea1o ng\u00e0y l\u1ec5 t\u1eeb h\u00f4m nay tr\u1edf \u0111i." });

            var existing = await _context.Holidays.IgnoreQueryFilters().FirstOrDefaultAsync(h => h.Date == model.Date);
            if (existing != null)
            {
                if (!existing.IsActive)
                    return Json(new { success = false, message = "Ngày này đã từng là ngày lễ và đang bị ẩn. Vui lòng khôi phục thay vì tạo mới." });
                return Json(new { success = false, message = "Ngày này đã được thiết lập là ngày lễ." });
            }

            model.CreatedAtUtc = DateTime.UtcNow;
            model.IsActive = true;
            _context.Holidays.Add(model);

            // TỰ ĐỘNG TẠO CHẤM CÔNG CHO TẤT CẢ GIÁO VIÊN
            var teachers = await _context.Employees
                .Include(e => e.Account)
                .Where(e => e.Account.IsActive && e.Account.Role.Name == "Employee")
                .ToListAsync();

            foreach (var t in teachers)
            {
                var attendance = await _context.WorkAttendances
                    .FirstOrDefaultAsync(w => w.EmployeeId == t.Id && w.Date == model.Date);

                if (attendance == null)
                {
                    _context.WorkAttendances.Add(new WorkAttendance
                    {
                        EmployeeId = t.Id,
                        Date = model.Date,
                        Status = "Approved",
                        WorkUnit = 1.0m,
                        Note = $"Nghỉ lễ: {model.Name}",
                        ReviewNote = "Hệ thống tự động tạo từ lịch nghỉ lễ"
                    });
                }
                else
                {
                    attendance.Status = "Approved";
                    attendance.WorkUnit = 1.0m;
                    attendance.Note = $"Nghỉ lễ: {model.Name} (Ghi đè)";
                }
            }

            await _context.SaveChangesAsync();

            // GỬI THÔNG BÁO CHO TOÀN BỘ GIÁO VIÊN VÀ PHỤ HUYNH
            var holidayMessage = $"Tr\u01b0\u1eddng s\u1ebd ngh\u1ec9 l\u1ec5 '{model.Name}' v\u00e0o ng\u00e0y {model.Date:dd/MM/yyyy}. Ch\u00fac c\u00e1c b\u1ea1n c\u00f3 m\u1ed9t k\u1ef3 ngh\u1ec9 vui v\u1ebb!";
            await _notificationService.SendToRoleAsync(
                "Employee",
                "Th\u00f4ng b\u00e1o ngh\u1ec9 l\u1ec5",
                holidayMessage,
                "info",
                $"/Employee/HolidayDetail/{model.Id}"
            );
            await _notificationService.SendToRoleAsync(
                "Parent",
                "Th\u00f4ng b\u00e1o ngh\u1ec9 l\u1ec5",
                holidayMessage,
                "info",
                $"/Parent/HolidayDetail/{model.Id}"
            );
            await _notificationService.SendToRoleAsync(
                "Manager",
                "Th\u00f4ng b\u00e1o ngh\u1ec9 l\u1ec5",
                holidayMessage,
                "info",
                "/HolidayManagement"
            );

            return Json(new { success = true, message = "Đã thiết lập ngày lễ, tự động tính công và gửi thông báo thành công." });
        }

        [HttpDelete("Api/Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var holiday = await _context.Holidays.FindAsync(id);
            if (holiday == null) return Json(new { success = false, message = "Không tìm thấy ngày lễ." });

            // Soft Delete: Chuyển IsActive thành false
            holiday.IsActive = false;

            // Đồng thời thu hồi chấm công tự động của ngày đó
            var attendances = await _context.WorkAttendances
                .Where(w => w.Date == holiday.Date && w.ReviewNote == "Hệ thống tự động tạo từ lịch nghỉ lễ")
                .ToListAsync();

            _context.WorkAttendances.RemoveRange(attendances);

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã ẩn ngày lễ và thu hồi các bản ghi chấm công tự động." });
        }

        [HttpPost("Api/Reactivate/{id}")]
        public async Task<IActionResult> Reactivate(int id)
        {
            var holiday = await _context.Holidays.IgnoreQueryFilters().FirstOrDefaultAsync(h => h.Id == id);
            if (holiday == null) return Json(new { success = false, message = "Không tìm thấy." });

            holiday.IsActive = true;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã khôi phục ngày lễ thành công." });
        }
    }
}
