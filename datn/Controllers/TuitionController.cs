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
            ViewData["Title"] = "Quản lý học phí";
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
            ViewData["Title"] = "Danh mục khoản thu";
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

            if (tuition == null) return NotFound("KhÃ´ng tÃ¬m tháº¥y thÃ´ng tin há»c phÃ­.");
            if (tuition.IsPaid) return BadRequest("Há»c phÃ­ nÃ y Ä‘Ã£ Ä‘Æ°á»£c thanh toÃ¡n.");

            // TÃ­nh tá»•ng tiá»n dá»±a trÃªn chi tiáº¿t
            decimal amount = tuition.TuitionDetails.Sum(d => d.TotalAmount) + (tuition.ExtraFee ?? 0);

            if (amount <= 0) return BadRequest("Sá»‘ tiá»n khÃ´ng há»£p lá»‡.");

            string orderInfo = $"Thanh toan hoc phi thang {tuition.Month}/{tuition.Year} cho be {tuition.Student?.FirstName} {tuition.Student?.LastName}";
            
            try
            {
                var payUrl = await _momoService.CreatePaymentAsync(tuition, amount, orderInfo);
                if (!string.IsNullOrEmpty(payUrl))
                {
                    return Json(new { success = true, url = payUrl });
                }
                return Json(new { success = false, message = "KhÃ´ng thá»ƒ táº¡o link thanh toÃ¡n MoMo." });
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
                ViewBag.Message = "Thanh toÃ¡n thÃ nh cÃ´ng! Cáº£m Æ¡n quÃ½ phá»¥ huynh.";
                ViewBag.Type = "success";
            }
            else
            {
                ViewBag.Message = $"Thanh toÃ¡n tháº¥t báº¡i hoáº·c Ä‘Ã£ bá»‹ há»§y. Lá»—i: {message}";
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

                                // ThÃ´ng bÃ¡o cho phá»¥ huynh (vÃ  cÃ³ thá»ƒ cho quáº£n lÃ½)
                                var parentStudent = await _context.ParentStudents.Include(ps => ps.Parent)
                                    .FirstOrDefaultAsync(ps => ps.StudentId == tuition.StudentId);
                                if (parentStudent != null && parentStudent.Parent != null)
                                {
                                    await _notificationService.SendToUserAsync(parentStudent.Parent.AccountId, 
                                        "XÃ¡c nháº­n Ä‘Ã£ Ä‘Ã³ng há»c phÃ­", 
                                        $"Há»‡ thá»‘ng Ä‘Ã£ nháº­n Ä‘Æ°á»£c há»c phÃ­ thÃ¡ng {tuition.Month}/{tuition.Year} qua MoMo cho bÃ© {tuition.Student?.FirstName} {tuition.Student?.LastName}. Cáº£m Æ¡n quÃ½ phá»¥ huynh.",
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
        public async Task<IActionResult> GetTuitionMonitoring(int month, int year, bool? isPaid, string? search, int? classId)
        {
            var query = _context.Tuitions
                .AsNoTracking()
                .Where(t => t.Month == month && t.Year == year && t.Student != null);

            if (isPaid.HasValue)
                query = query.Where(t => t.IsPaid == isPaid.Value);

            if (classId.HasValue && classId.Value > 0)
                query = query.Where(t => t.Student!.ClassId == classId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                var namePattern = $"%{keyword}%";
                var isNumericSearch = int.TryParse(keyword.TrimStart('#'), out var searchedId);

                query = query.Where(t =>
                    EF.Functions.Like((t.Student!.FirstName + " " + t.Student.LastName).Trim(), namePattern) ||
                    EF.Functions.Like((t.Student.LastName + " " + t.Student.FirstName).Trim(), namePattern) ||
                    (isNumericSearch && t.Student.Id == searchedId));
            }

            var result = await query
                .OrderBy(t => t.Student!.LastName)
                .ThenBy(t => t.Student!.FirstName)
                .Select(t => new
                {
                    id = t.Id,
                    studentId = t.Student!.Id,
                    studentName = ((t.Student.FirstName ?? "") + " " + (t.Student.LastName ?? "")).Trim(),
                    className = t.Student.Class != null ? t.Student.Class.Name : "Chưa phân lớp",
                    amount = t.TuitionDetails.Sum(d => d.TotalAmount),
                    extraFee = t.ExtraFee ?? 0,
                    total = t.TuitionDetails.Sum(d => d.TotalAmount) + (t.ExtraFee ?? 0),
                    isPaid = t.IsPaid
                })
                .ToListAsync();

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
            // Kiá»ƒm tra xem Ä‘Ã£ khá»Ÿi táº¡o há»c phÃ­ cho thÃ¡ng nÃ y chÆ°a
            var existingTuitionCount = await _context.Tuitions.CountAsync(t => t.Month == month && t.Year == year);
            if (existingTuitionCount > 0)
            {
                return Json(new { success = false, message = $"Há»c phÃ­ thÃ¡ng {month}/{year} Ä‘Ã£ Ä‘Æ°á»£c khá»Ÿi táº¡o trÆ°á»›c Ä‘Ã³. KhÃ´ng thá»ƒ khá»Ÿi táº¡o láº¡i." });
            }

            // 1. Láº¥y danh sÃ¡ch há»c sinh Ä‘ang hoáº¡t Ä‘á»™ng
            var students = await _context.Students
                .Include(s => s.Class)
                .Include(s => s.StudentFeeConfigs).ThenInclude(c => c.FeeItem)
                .Where(s => s.Status == StudentStatus.Active)
                .ToListAsync();

            if (!students.Any())
            {
                return Json(new { success = false, message = "ChÆ°a cÃ³ há»c sinh nÃ o Ä‘ang hoáº¡t Ä‘á»™ng Ä‘á»ƒ khá»Ÿi táº¡o há»c phÃ­." });
            }

            // 2. Láº¥y danh sÃ¡ch cÃ¡c khoáº£n thu báº¯t buá»™c (toÃ n trÆ°á»ng)
            var requiredFeeItems = await _context.FeeItems
                .Where(f => f.IsActive && f.IsRequired)
                .ToListAsync();

            if (!requiredFeeItems.Any())
            {
                return Json(new { success = false, message = "ChÆ°a cÃ³ cáº¥u hÃ¬nh khoáº£n thu báº¯t buá»™c nÃ o cho trÆ°á»ng. Vui lÃ²ng vÃ o Danh má»¥c khoáº£n thu Ä‘á»ƒ thiáº¿t láº­p trÆ°á»›c khi khá»Ÿi táº¡o há»c phÃ­." });
            }
            int count = 0;

            foreach (var student in students)
            {
                // Kiá»ƒm tra xem Ä‘Ã£ cÃ³ hÃ³a Ä‘Æ¡n cho thÃ¡ng nÃ y chÆ°a
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
                    // Náº¿u Ä‘Ã£ thanh toÃ¡n rá»“i thÃ¬ khÃ´ng ghi Ä‘Ã¨ láº¡i dá»¯ liá»‡u Ä‘á»ƒ trÃ¡nh sai lá»‡ch káº¿ toÃ¡n
                    continue;
                }
                else
                {
                    // Náº¿u chÆ°a thanh toÃ¡n, xÃ³a cÃ¡c chi tiáº¿t cÅ© Ä‘á»ƒ tÃ­nh toÃ¡n láº¡i tá»« Ä‘áº§u (Ä‘áº£m báº£o cáº­p nháº­t giÃ¡ má»›i nháº¥t)
                    _context.TuitionDetails.RemoveRange(tuition.TuitionDetails);
                    tuition.TuitionDetails.Clear();
                }

                // -- A. ThÃªm cÃ¡c khoáº£n thu báº¯t buá»™c --
                foreach (var item in requiredFeeItems)
                {
                    var today = DateOnly.FromDateTime(DateTime.Today);
                    if (item.AgeFrom == 2 && item.AgeTo == 3)
                    {
                        var studentAgeInMonths = CalculateAgeInMonths(student.DateOfBirth, today);
                        if (studentAgeInMonths < 24 || studentAgeInMonths > 36) continue;
                    }
                    else
                    {
                        var studentAge = CalculateAgeInYears(student.DateOfBirth, today);
                        if (item.AgeFrom.HasValue && studentAge < item.AgeFrom.Value) continue;
                        if (item.AgeTo.HasValue && studentAge > item.AgeTo.Value) continue;
                    }

                    tuition.TuitionDetails.Add(new TuitionDetail
                    {
                        FeeItemId = item.Id,
                        Name = item.Name,
                        Amount = item.DefaultAmount,
                        DiscountAmount = 0,
                        TotalAmount = item.DefaultAmount
                    });
                }
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

                    // 4. ThÃ´ng bÃ¡o cho phá»¥ huynh (Chá»‰ gá»­i thÃ´ng bÃ¡o khi táº¡o má»›i hÃ³a Ä‘Æ¡n)
                    var parentStudent = await _context.ParentStudents.Include(ps => ps.Parent)
                        .FirstOrDefaultAsync(ps => ps.StudentId == student.Id);
                    if (parentStudent != null && parentStudent.Parent != null)
                    {
                        await _notificationService.SendToUserAsync(parentStudent.Parent.AccountId, 
                            "ThÃ´ng bÃ¡o há»c phÃ­ má»›i", 
                            $"Há»c phÃ­ thÃ¡ng {month}/{year} cá»§a bÃ© {student.FirstName} {student.LastName} Ä‘Ã£ Ä‘Æ°á»£c khá»Ÿi táº¡o. Vui lÃ²ng kiá»ƒm tra vÃ  hoÃ n thÃ nh ná»™p phÃ­.",
                            "info", "/Tuition/MyTuition");
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"ÄÃ£ sinh {count} hÃ³a Ä‘Æ¡n há»c phÃ­ chi tiáº¿t cho thÃ¡ng {month}/{year}." });
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
            if (item == null) return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y khoáº£n thu." });
            return Json(new { success = true, data = item });
        }

        [Authorize(Roles = "Manager")]
        [HttpPost("Api/FeeItem")]
        public async Task<IActionResult> CreateFeeItem([FromBody] SaveFeeItemViewModel model)
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = "Dá»¯ liá»‡u khÃ´ng há»£p lá»‡." });

            var exists = await _context.FeeItems.AnyAsync(f => f.Name == model.Name.Trim());
            if (exists) return Json(new { success = false, message = "TÃªn khoáº£n thu Ä‘Ã£ tá»“n táº¡i." });

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
            return Json(new { success = true, message = "ThÃªm khoáº£n thu thÃ nh cÃ´ng." });
        }

        [Authorize(Roles = "Manager")]
        [HttpPut("Api/FeeItem/{id}")]
        public async Task<IActionResult> UpdateFeeItem(int id, [FromBody] SaveFeeItemViewModel model)
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = "Dá»¯ liá»‡u khÃ´ng há»£p lá»‡." });

            var item = await _context.FeeItems.FindAsync(id);
            if (item == null) return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y khoáº£n thu." });

            var exists = await _context.FeeItems.AnyAsync(f => f.Id != id && f.Name == model.Name.Trim());
            if (exists) return Json(new { success = false, message = "TÃªn khoáº£n thu Ä‘Ã£ tá»“n táº¡i." });

            item.Name = model.Name.Trim();
            item.Description = model.Description?.Trim();
            item.DefaultAmount = model.DefaultAmount;
            item.AgeFrom = model.AgeFrom;
            item.AgeTo = model.AgeTo;
            item.IsRequired = model.IsRequired;
            item.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cáº­p nháº­t thÃ nh cÃ´ng." });
        }

        [Authorize(Roles = "Manager")]
        [HttpDelete("Api/FeeItem/{id}")]
        public async Task<IActionResult> DeleteFeeItem(int id)
        {
            var item = await _context.FeeItems.FindAsync(id);
            if (item == null) return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y." });

            var usedInConfig = await _context.StudentFeeConfigs.AnyAsync(s => s.FeeItemId == id);
            var usedInTuition = await _context.TuitionDetails.AnyAsync(t => t.FeeItemId == id);

            if (usedInConfig || usedInTuition)
            {
                item.IsActive = false;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Khoáº£n thu Ä‘ang Ä‘Æ°á»£c sá»­ dá»¥ng nÃªn Ä‘Ã£ Ä‘Æ°á»£c chuyá»ƒn sang tráº¡ng thÃ¡i NgÆ°ng hoáº¡t Ä‘á»™ng." });
            }

            _context.FeeItems.Remove(item);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "XÃ³a khoáº£n thu thÃ nh cÃ´ng." });
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
            if (!ModelState.IsValid) return Json(new { success = false, message = "Dá»¯ liá»‡u khÃ´ng há»£p lá»‡." });

            StudentFeeConfig? config;
            if (model.Id > 0)
            {
                config = await _context.StudentFeeConfigs.FindAsync(model.Id);
                if (config == null) return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y cáº¥u hÃ¬nh." });
            }
            else
            {
                // Kiá»ƒm tra xem Ä‘Ã£ cÃ³ cáº¥u hÃ¬nh cho khoáº£n phÃ­ nÃ y chÆ°a
                var exists = await _context.StudentFeeConfigs.AnyAsync(s => s.StudentId == model.StudentId && s.FeeItemId == model.FeeItemId);
                if (exists) return Json(new { success = false, message = "Khoáº£n thu nÃ y Ä‘Ã£ Ä‘Æ°á»£c Ä‘Äƒng kÃ½ cho há»c sinh." });

                config = new StudentFeeConfig { StudentId = model.StudentId, FeeItemId = model.FeeItemId };
                _context.StudentFeeConfigs.Add(config);
            }

            config.CustomAmount = model.CustomAmount;
            config.DiscountAmount = model.DiscountAmount;
            config.DiscountPercentage = model.DiscountPercentage;
            config.Note = model.Note;
            config.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "LÆ°u cáº¥u hÃ¬nh phÃ­ thÃ nh cÃ´ng." });
        }

        [Authorize(Roles = "Manager")]
        [HttpDelete("Api/StudentFinance/{id}")]
        public async Task<IActionResult> DeleteStudentFinance(int id)
        {
            var config = await _context.StudentFeeConfigs.FindAsync(id);
            if (config == null) return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y." });

            _context.StudentFeeConfigs.Remove(config);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "ÄÃ£ xÃ³a Ä‘Äƒng kÃ½ khoáº£n thu." });
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
    }
}



