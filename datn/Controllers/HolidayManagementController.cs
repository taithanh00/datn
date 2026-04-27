using datn.Data;
using datn.Models;
using datn.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace datn.Controllers
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
            ViewData["Title"] = "Quản lý Ngày lễ";
            return View();
        }

        [HttpGet("Api/List")]
        public async Task<IActionResult> List()
        {
            var holidays = await _context.Holidays
                .OrderByDescending(h => h.Date)
                .ToListAsync();
            return Json(new { success = true, data = holidays });
        }

        [HttpPost("Api/Create")]
        public async Task<IActionResult> Create([FromBody] Holiday model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
                return Json(new { success = false, message = "Vui lòng nhập tên ngày lễ." });

            var existing = await _context.Holidays.FirstOrDefaultAsync(h => h.Date == model.Date);
            if (existing != null)
                return Json(new { success = false, message = "Ngày này đã được thiết lập là ngày lễ." });

            model.CreatedAtUtc = DateTime.UtcNow;
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
            await _notificationService.SendToAllAsync(
                "Thông báo nghỉ lễ",
                $"Trường sẽ nghỉ lễ '{model.Name}' vào ngày {model.Date:dd/MM/yyyy}. Chúc các bạn có một kỳ nghỉ vui vẻ!",
                "info", "/Employee/WorkSchedule"
            );

            return Json(new { success = true, message = "Đã thiết lập ngày lễ, tự động tính công và gửi thông báo thành công." });
        }

        [HttpDelete("Api/Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var holiday = await _context.Holidays.FindAsync(id);
            if (holiday == null) return Json(new { success = false, message = "Không tìm thấy ngày lễ." });

            // Khi xóa ngày lễ, có nên xóa chấm công tự động không?
            // Để an toàn, ta nên hỏi Manager. Ở đây ta thực hiện xóa chấm công tự động của ngày đó.
            var attendances = await _context.WorkAttendances
                .Where(w => w.Date == holiday.Date && w.ReviewNote == "Hệ thống tự động tạo từ lịch nghỉ lễ")
                .ToListAsync();

            _context.WorkAttendances.RemoveRange(attendances);
            _context.Holidays.Remove(holiday);

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã xóa ngày lễ và thu hồi các bản ghi chấm công tự động." });
        }
    }
}
