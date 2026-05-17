using datn.Data;
using datn.Models;
using datn.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

namespace datn.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class TuitionController : BaseController
    {
        private readonly INotificationService _notificationService; 
        private readonly IMoMoService _momoService;
        private readonly IConfiguration _config;

        public TuitionController(AppDbContext context, INotificationService notificationService, IMoMoService momoService, IConfiguration config) : base(context)
        {
            _notificationService = notificationService;
            _momoService = momoService;
            _config = config;
        }

        // ============ MANAGER VIEWS ============

        [Authorize(Roles = "Manager")]
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Quản lý Học phí";
            var mandatoryFees = await _context.FeeItems
                .Where(f => f.IsActive && f.IsRequired)
                .OrderBy(f => f.AgeFrom)
                .ToListAsync();
            return View(mandatoryFees);
        }

        [Authorize(Roles = "Manager")]
        [HttpGet("Monitoring")]
        public IActionResult Monitoring()
        {
            ViewData["Title"] = "Theo dõi nộp học phí";
            return View();
        }

        [Authorize(Roles = "Manager")]
        [HttpGet("FeeItems")]
        public IActionResult FeeItems()
        {
            ViewData["Title"] = "Danh mục Khoản thu";
            return View();
        }

        // ============ PARENT VIEWS ============

        [Authorize(Roles = "Parent")]
        [HttpGet("MyTuition")]
        public async Task<IActionResult> MyTuition()
        {
            ViewData["Title"] = "Học phí của con";
            var username = User.Identity?.Name;
            var parent = await _context.Parents.Include(p => p.Account)
                .Include(p => p.ParentStudents).ThenInclude(ps => ps.Student)
                .FirstOrDefaultAsync(p => p.Account.Username == username);

            if (parent == null) return NotFound();

            var studentIds = parent.ParentStudents.Select(ps => ps.StudentId).ToList();
            var tuitions = await _context.Tuitions
                .Include(t => t.Student)
                .Include(t => t.TuitionDetails)
                .Where(t => studentIds.Contains(t.StudentId ?? 0))
                .OrderByDescending(t => t.Year).ThenByDescending(t => t.Month)
                .ToListAsync();
            return View(tuitions);
        }

        [Authorize(Roles = "Parent")]
        [HttpPost("CreateMoMoPayment/{id}")]
        public async Task<IActionResult> CreateMoMoPayment(int id)
        {
            var username = User.Identity?.Name;
            var parent = await _context.Parents.Include(p => p.Account)
                .Include(p => p.ParentStudents).ThenInclude(ps => ps.Student)
                .FirstOrDefaultAsync(p => p.Account.Username == username);

            if (parent == null) return NotFound();

            var studentIds = parent.ParentStudents.Select(ps => ps.StudentId).ToList();
            
            var tuition = await _context.Tuitions
                .Include(t => t.Student)
                .Include(t => t.TuitionDetails)
                .FirstOrDefaultAsync(t => t.Id == id && studentIds.Contains(t.StudentId ?? 0));

            if (tuition == null) return NotFound("Không tìm thấy thông tin học phí.");
            if (tuition.IsPaid) return BadRequest("Học phí này đã được thanh toán.");

            // Tính tổng tiền dựa trên chi tiết
            decimal amount = tuition.TuitionDetails.Sum(d => d.TotalAmount) + (tuition.ExtraFee ?? 0);

            if (amount <= 0) return BadRequest("Số tiền không hợp lệ.");

            string orderInfo = $"Thanh toan hoc phi thang {tuition.Month}/{tuition.Year} cho be {tuition.Student?.FirstName} {tuition.Student?.LastName}";
            
            try
            {
                var payUrl = await _momoService.CreatePaymentAsync(tuition, amount, orderInfo);
                if (!string.IsNullOrEmpty(payUrl))
                {
                    return Json(new { success = true, url = payUrl });
                }
                return Json(new { success = false, message = "Không thể tạo link thanh toán MoMo." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("MoMoReturn")]
        public async Task<IActionResult> MoMoReturn([FromQuery] string partnerCode, [FromQuery] string orderId, [FromQuery] string requestId, 
            [FromQuery] string amount, [FromQuery] string orderInfo, [FromQuery] string orderType, [FromQuery] string transId, 
            [FromQuery] string resultCode, [FromQuery] string message, [FromQuery] string payType, [FromQuery] string responseTime, 
            [FromQuery] string extraData, [FromQuery] string signature)
        {
            // Note: In a real app, you should validate the signature here as well.
            // But since IPN is the source of truth, we just show the result to the user.
            
            if (resultCode == "0")
            {
                ViewBag.Message = "Thanh toán thành công! Cảm ơn quý phụ huynh.";
                ViewBag.Type = "success";
            }
            else
            {
                ViewBag.Message = $"Thanh toán thất bại hoặc đã bị hủy. Lỗi: {message}";
                ViewBag.Type = "danger";
            }

            return View();
        }

        [AllowAnonymous]
        [HttpPost("MoMoIPN")]
        public async Task<IActionResult> MoMoIPN([FromBody] System.Text.Json.JsonElement requestBody)
        {
            try
            {
                string partnerCode = requestBody.GetProperty("partnerCode").GetString() ?? "";
                string orderId = requestBody.GetProperty("orderId").GetString() ?? "";
                string requestId = requestBody.GetProperty("requestId").GetString() ?? "";
                string amountStr = requestBody.GetProperty("amount").GetRawText() ?? "0";
                string orderInfo = requestBody.GetProperty("orderInfo").GetString() ?? "";
                string orderType = requestBody.GetProperty("orderType").GetString() ?? "";
                string transId = requestBody.GetProperty("transId").GetRawText() ?? "0";
                string resultCode = requestBody.GetProperty("resultCode").GetRawText() ?? "-1";
                string message = requestBody.GetProperty("message").GetString() ?? "";
                string payType = requestBody.GetProperty("payType").GetString() ?? "";
                string responseTime = requestBody.GetProperty("responseTime").GetRawText() ?? "";
                string extraData = requestBody.GetProperty("extraData").GetString() ?? "";
                string signature = requestBody.GetProperty("signature").GetString() ?? "";

                // Compute raw hash for IPN validation
                string rawHash = $"accessKey={_config.GetSection("MoMo")["AccessKey"]}&amount={amountStr}&extraData={extraData}&message={message}&orderId={orderId}&orderInfo={orderInfo}&orderType={orderType}&partnerCode={partnerCode}&payType={payType}&requestId={requestId}&responseTime={responseTime}&resultCode={resultCode}&transId={transId}";
                
                bool isValid = _momoService.ValidateSignature(signature, rawHash);
                
                if (!isValid)
                {
                    return BadRequest("Invalid signature");
                }

                if (resultCode == "0") // Success
                {
                    string plainExtraData = Encoding.UTF8.GetString(Convert.FromBase64String(extraData));
                    // Parse TuitionId from extraData (e.g., "TuitionId=123")
                    if (extraData.StartsWith("TuitionId="))
                    {
                        if (int.TryParse(extraData.Substring(10), out int tuitionId))
                        {
                            var tuition = await _context.Tuitions.Include(t => t.Student).FirstOrDefaultAsync(t => t.Id == tuitionId);
                            if (tuition != null && !tuition.IsPaid)
                            {
                                tuition.IsPaid = true;
                                tuition.PaymentMethod = "MoMo";
                                tuition.TransactionId = transId;
                                tuition.PaidAt = DateTime.UtcNow;
                                tuition.PaymentStatus = "Success";
                                
                                await _context.SaveChangesAsync();

                                // Thông báo cho phụ huynh (và có thể cho quản lý)
                                var parentStudent = await _context.ParentStudents.Include(ps => ps.Parent)
                                    .FirstOrDefaultAsync(ps => ps.StudentId == tuition.StudentId);
                                if (parentStudent != null && parentStudent.Parent != null)
                                {
                                    await _notificationService.SendToUserAsync(parentStudent.Parent.AccountId, 
                                        "Xác nhận đã đóng học phí", 
                                        $"Hệ thống đã nhận được học phí tháng {tuition.Month}/{tuition.Year} qua MoMo cho bé {tuition.Student?.FirstName} {tuition.Student?.LastName}. Cảm ơn quý phụ huynh.",
                                        "success", "/Tuition/MyTuition");
                                }
                            }
                        }
                    }
                }

                return NoContent(); // Standard response for IPN
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        // ============ API ENDPOINTS ============

        [Authorize(Roles = "Manager")]
        [HttpGet("Api/Monitoring")]
        public async Task<IActionResult> GetTuitionMonitoring(int month, int year, bool? isPaid)
        {
            var query = _context.Tuitions
                .Include(t => t.Student).ThenInclude(s => s.Class)
                .Include(t => t.TuitionDetails)
                .Where(t => t.Month == month && t.Year == year);

            if (isPaid.HasValue)
                query = query.Where(t => t.IsPaid == isPaid.Value);

            var data = await query.OrderBy(t => t.Student.LastName).ThenBy(t => t.Student.FirstName).ToListAsync();
            
            var result = data.Select(t => new {
                id = t.Id,
                studentName = (t.Student.FirstName + " " + t.Student.LastName).Trim(),
                className = t.Student.Class?.Name ?? "Chưa phân lớp",
                amount = t.TuitionDetails.Sum(d => d.TotalAmount),
                extraFee = t.ExtraFee ?? 0,
                total = t.TuitionDetails.Sum(d => d.TotalAmount) + (t.ExtraFee ?? 0),
                isPaid = t.IsPaid
            });

            return Json(new { success = true, data = result });
        }

        [Authorize(Roles = "Manager")]
        [HttpGet("Api/TuitionDetails/{id}")]
        public async Task<IActionResult> GetTuitionDetails(int id)
        {
            var details = await _context.TuitionDetails
                .Where(d => d.TuitionId == id)
                .ToListAsync();
            return Json(new { success = true, data = details });
        }

        [Authorize(Roles = "Manager")]
        [HttpPost("Api/GenerateMonthlyTuition")]
        public async Task<IActionResult> GenerateMonthlyTuition(int month, int year)
        {
            // Kiểm tra xem đã khởi tạo học phí cho tháng này chưa
            var existingTuitionCount = await _context.Tuitions.CountAsync(t => t.Month == month && t.Year == year);
            if (existingTuitionCount > 0)
            {
                return Json(new { success = false, message = $"Học phí tháng {month}/{year} đã được khởi tạo trước đó. Không thể khởi tạo lại." });
            }

            // 1. Lấy danh sách học sinh đang hoạt động
            var students = await _context.Students
                .Include(s => s.Class)
                .Include(s => s.StudentFeeConfigs).ThenInclude(c => c.FeeItem)
                .Where(s => s.Status == StudentStatus.Active)
                .ToListAsync();

            if (!students.Any())
            {
                return Json(new { success = false, message = "Chưa có học sinh nào đang hoạt động để khởi tạo học phí." });
            }

            // 2. Lấy danh sách các khoản thu bắt buộc (toàn trường)
            var requiredFeeItems = await _context.FeeItems
                .Where(f => f.IsActive && f.IsRequired)
                .ToListAsync();

            if (!requiredFeeItems.Any())
            {
                return Json(new { success = false, message = "Chưa có cấu hình khoản thu bắt buộc nào cho trường. Vui lòng vào Danh mục khoản thu để thiết lập trước khi khởi tạo học phí." });
            }

            // 3. Lấy danh sách môn học có phí (để check đăng ký nếu cần - Tương lai)
            var subjectWithFees = await _context.Subjects
                .Where(s => s.IsActive && s.FeeAmount > 0)
                .ToListAsync();

            int count = 0;

            foreach (var student in students)
            {
                // Kiểm tra xem đã có hóa đơn cho tháng này chưa
                var tuition = await _context.Tuitions
                    .Include(t => t.TuitionDetails)
                    .FirstOrDefaultAsync(t => t.StudentId == student.Id && t.Month == month && t.Year == year);
                
                bool isNew = false;
                if (tuition == null)
                {
                    isNew = true;
                    tuition = new Tuition
                    {
                        StudentId = student.Id,
                        Month = month,
                        Year = year,
                        IsPaid = false,
                        ExtraFee = 0,
                        TuitionDetails = new List<TuitionDetail>()
                    };
                }
                else if (tuition.IsPaid)
                {
                    // Nếu đã thanh toán rồi thì không ghi đè lại dữ liệu để tránh sai lệch kế toán
                    continue;
                }
                else
                {
                    // Nếu chưa thanh toán, xóa các chi tiết cũ để tính toán lại từ đầu (đảm bảo cập nhật giá mới nhất)
                    _context.TuitionDetails.RemoveRange(tuition.TuitionDetails);
                    tuition.TuitionDetails.Clear();
                }

                // -- A. Thêm các khoản thu bắt buộc --
                foreach (var item in requiredFeeItems)
                {
                    // Tính toán độ tuổi phù hợp
                    int studentAge = DateTime.Now.Year - student.DateOfBirth.Year;
                    
                    // Nếu khoản phí có quy định độ tuổi, kiểm tra xem học sinh có nằm trong khoảng đó không
                    if (item.AgeFrom.HasValue && studentAge < item.AgeFrom.Value) continue;
                    if (item.AgeTo.HasValue && studentAge > item.AgeTo.Value) continue;

                    tuition.TuitionDetails.Add(new TuitionDetail
                    {
                        FeeItemId = item.Id,
                        Name = item.Name,
                        Amount = item.DefaultAmount,
                        DiscountAmount = 0,
                        TotalAmount = item.DefaultAmount
                    });
                }

                // -- B. Thêm các khoản thu riêng của học sinh (Xe bus, ăn uống, môn năng khiếu...) --
                foreach (var config in student.StudentFeeConfigs.Where(c => c.FeeItem.IsActive))
                {
                    var baseAmount = config.CustomAmount ?? config.FeeItem.DefaultAmount;
                    var discount = (baseAmount * config.DiscountPercentage / 100) + config.DiscountAmount;
                    var final = baseAmount - discount;

                    var existingDetail = tuition.TuitionDetails.FirstOrDefault(d => d.FeeItemId == config.FeeItemId);
                    if (existingDetail != null)
                    {
                        existingDetail.Amount = baseAmount;
                        existingDetail.DiscountAmount = discount;
                        existingDetail.TotalAmount = final;
                    }
                    else
                    {
                        tuition.TuitionDetails.Add(new TuitionDetail
                        {
                            FeeItemId = config.FeeItemId,
                            Name = config.FeeItem.Name,
                            Amount = baseAmount,
                            DiscountAmount = discount,
                            TotalAmount = final
                        });
                    }
                }

                if (isNew)
                {
                    _context.Tuitions.Add(tuition);
                    count++;

                    // 4. Thông báo cho phụ huynh (Chỉ gửi thông báo khi tạo mới hóa đơn)
                    var parentStudent = await _context.ParentStudents.Include(ps => ps.Parent)
                        .FirstOrDefaultAsync(ps => ps.StudentId == student.Id);
                    if (parentStudent != null && parentStudent.Parent != null)
                    {
                        await _notificationService.SendToUserAsync(parentStudent.Parent.AccountId, 
                            "Thông báo học phí mới", 
                            $"Học phí tháng {month}/{year} của bé {student.FirstName} {student.LastName} đã được khởi tạo. Vui lòng kiểm tra và hoàn thành nộp phí.",
                            "info", "/Tuition/MyTuition");
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"Đã sinh {count} hóa đơn học phí chi tiết cho tháng {month}/{year}." });
        }

        [Authorize(Roles = "Manager")]
        [HttpPost("Api/ConfirmPaid/{id}")]
        public async Task<IActionResult> ConfirmPaid(int id)
        {
            var tuition = await _context.Tuitions.Include(t => t.Student).FirstOrDefaultAsync(t => t.Id == id);
            if (tuition == null) return NotFound();

            tuition.IsPaid = true;
            await _context.SaveChangesAsync();

            // Thông báo cho phụ huynh
            var parentStudent = await _context.ParentStudents.Include(ps => ps.Parent)
                .FirstOrDefaultAsync(ps => ps.StudentId == tuition.StudentId);
            if (parentStudent != null)
            {
                await _notificationService.SendToUserAsync(parentStudent.Parent.AccountId, 
                    "Xác nhận đã đóng học phí", 
                    $"Hệ thống đã nhận được học phí tháng {tuition.Month}/{tuition.Year} cho bé {tuition.Student.FirstName} {tuition.Student.LastName}. Cảm ơn quý phụ huynh.",
                    "success", "/Tuition/MyTuition");
            }

            return Json(new { success = true, message = "Đã xác nhận thanh toán." });
        }

        // ============ FEE ITEM API ============

        [Authorize(Roles = "Manager")]
        [HttpGet("Api/FeeItems")]
        public async Task<IActionResult> GetFeeItems()
        {
            var items = await _context.FeeItems
                .OrderByDescending(f => f.IsRequired)
                .ThenBy(f => f.Name)
                .ToListAsync();
            return Json(new { success = true, data = items });
        }

        [Authorize(Roles = "Manager")]
        [HttpGet("Api/FeeItem/{id}")]
        public async Task<IActionResult> GetFeeItem(int id)
        {
            var item = await _context.FeeItems.FindAsync(id);
            if (item == null) return Json(new { success = false, message = "Không tìm thấy khoản thu." });
            return Json(new { success = true, data = item });
        }

        [Authorize(Roles = "Manager")]
        [HttpPost("Api/FeeItem")]
        public async Task<IActionResult> CreateFeeItem([FromBody] SaveFeeItemViewModel model)
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = "Dữ liệu không hợp lệ." });

            var exists = await _context.FeeItems.AnyAsync(f => f.Name == model.Name.Trim());
            if (exists) return Json(new { success = false, message = "Tên khoản thu đã tồn tại." });

            var item = new FeeItem
            {
                Name = model.Name.Trim(),
                Description = model.Description?.Trim(),
                DefaultAmount = model.DefaultAmount,
                AgeFrom = model.AgeFrom,
                AgeTo = model.AgeTo,
                IsRequired = model.IsRequired,
                IsActive = model.IsActive
            };

            _context.FeeItems.Add(item);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Thêm khoản thu thành công." });
        }

        [Authorize(Roles = "Manager")]
        [HttpPut("Api/FeeItem/{id}")]
        public async Task<IActionResult> UpdateFeeItem(int id, [FromBody] SaveFeeItemViewModel model)
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = "Dữ liệu không hợp lệ." });

            var item = await _context.FeeItems.FindAsync(id);
            if (item == null) return Json(new { success = false, message = "Không tìm thấy khoản thu." });

            var exists = await _context.FeeItems.AnyAsync(f => f.Id != id && f.Name == model.Name.Trim());
            if (exists) return Json(new { success = false, message = "Tên khoản thu đã tồn tại." });

            item.Name = model.Name.Trim();
            item.Description = model.Description?.Trim();
            item.DefaultAmount = model.DefaultAmount;
            item.AgeFrom = model.AgeFrom;
            item.AgeTo = model.AgeTo;
            item.IsRequired = model.IsRequired;
            item.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật thành công." });
        }

        [Authorize(Roles = "Manager")]
        [HttpDelete("Api/FeeItem/{id}")]
        public async Task<IActionResult> DeleteFeeItem(int id)
        {
            var item = await _context.FeeItems.FindAsync(id);
            if (item == null) return Json(new { success = false, message = "Không tìm thấy." });

            var usedInConfig = await _context.StudentFeeConfigs.AnyAsync(s => s.FeeItemId == id);
            var usedInTuition = await _context.TuitionDetails.AnyAsync(t => t.FeeItemId == id);

            if (usedInConfig || usedInTuition)
            {
                item.IsActive = false;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Khoản thu đang được sử dụng nên đã được chuyển sang trạng thái Ngưng hoạt động." });
            }

            _context.FeeItems.Remove(item);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Xóa khoản thu thành công." });
        }

        public class SaveFeeItemViewModel
        {
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public decimal DefaultAmount { get; set; }
            public int? AgeFrom { get; set; }
            public int? AgeTo { get; set; }
            public bool IsRequired { get; set; }
            public bool IsActive { get; set; }
        }

        // ============ STUDENT FINANCE CONFIG API ============

        [Authorize(Roles = "Manager")]
        [HttpGet("Api/StudentFinance/{studentId}")]
        public async Task<IActionResult> GetStudentFinance(int studentId)
        {
            var configs = await _context.StudentFeeConfigs
                .Include(s => s.FeeItem)
                .Where(s => s.StudentId == studentId)
                .ToListAsync();
            
            var result = configs.Select(c => new {
                id = c.Id,
                feeItemId = c.FeeItemId,
                feeName = c.FeeItem.Name,
                defaultAmount = c.FeeItem.DefaultAmount,
                customAmount = c.CustomAmount,
                discountAmount = c.DiscountAmount,
                discountPercentage = c.DiscountPercentage,
                note = c.Note,
                finalAmount = (c.CustomAmount ?? c.FeeItem.DefaultAmount) * (1 - c.DiscountPercentage / 100) - c.DiscountAmount
            });

            return Json(new { success = true, data = result });
        }

        [Authorize(Roles = "Manager")]
        [HttpPost("Api/StudentFinance")]
        public async Task<IActionResult> SaveStudentFinance([FromBody] SaveStudentFeeConfigViewModel model)
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = "Dữ liệu không hợp lệ." });

            StudentFeeConfig? config;
            if (model.Id > 0)
            {
                config = await _context.StudentFeeConfigs.FindAsync(model.Id);
                if (config == null) return Json(new { success = false, message = "Không tìm thấy cấu hình." });
            }
            else
            {
                // Kiểm tra xem đã có cấu hình cho khoản phí này chưa
                var exists = await _context.StudentFeeConfigs.AnyAsync(s => s.StudentId == model.StudentId && s.FeeItemId == model.FeeItemId);
                if (exists) return Json(new { success = false, message = "Khoản thu này đã được đăng ký cho học sinh." });

                config = new StudentFeeConfig { StudentId = model.StudentId, FeeItemId = model.FeeItemId };
                _context.StudentFeeConfigs.Add(config);
            }

            config.CustomAmount = model.CustomAmount;
            config.DiscountAmount = model.DiscountAmount;
            config.DiscountPercentage = model.DiscountPercentage;
            config.Note = model.Note;
            config.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Lưu cấu hình phí thành công." });
        }

        [Authorize(Roles = "Manager")]
        [HttpDelete("Api/StudentFinance/{id}")]
        public async Task<IActionResult> DeleteStudentFinance(int id)
        {
            var config = await _context.StudentFeeConfigs.FindAsync(id);
            if (config == null) return Json(new { success = false, message = "Không tìm thấy." });

            _context.StudentFeeConfigs.Remove(config);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã xóa đăng ký khoản thu." });
        }

        public class SaveStudentFeeConfigViewModel
        {
            public int Id { get; set; }
            public int StudentId { get; set; }
            public int FeeItemId { get; set; }
            public decimal? CustomAmount { get; set; }
            public decimal DiscountAmount { get; set; }
            public decimal DiscountPercentage { get; set; }
            public string? Note { get; set; }
        }
    }
}
