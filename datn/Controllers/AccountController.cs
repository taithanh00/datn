using datn.Data;
using datn.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace datn.Controllers
{
    /// <summary>
    /// AccountController: Xử lý các request liên quan tới tài khoản người dùng
    /// 
    /// Đặc điểm:
    /// - Tất cả action đều được bảo vệ bằng [Authorize] - yêu cầu user phải đăng nhập
    /// - Profile action sẽ hiển thị/cập nhật thông tin cá nhân của user đang đăng nhập
    /// - Thông tin khác nhau tùy theo role (Employee hoặc Parent)
    /// </summary>
    [Authorize]
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET: /Account/Profile
        /// Hiển thị form chỉnh sửa thông tin cá nhân của user đang đăng nhập
        /// 
        /// Luồng:
        /// 1. Lấy AccountId từ JWT Claims
        /// 2. Query Account + Role + Employee hoặc Parent tương ứng
        /// 3. Truyền dữ liệu vào view để hiển thị form
        /// </summary>
        public async Task<IActionResult> Profile()
        {
            // Lấy AccountId từ JWT Claims (NameIdentifier)
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(accountIdClaim, out int accountId))
            {
                return BadRequest("Không thể xác định tài khoản");
            }

            // Query Account từ database
            var account = await _context.Accounts
                .Include(a => a.Role)
                .Include(a => a.Employee)
                .Include(a => a.Parent)
                .FirstOrDefaultAsync(a => a.Id == accountId && a.IsActive);

            if (account == null)
            {
                return NotFound("Tài khoản không tồn tại");
            }

            // Truyền data vào ViewBag
            ViewBag.Username = account.Username;
            ViewBag.Email = account.Email;
            ViewBag.Role = account.Role?.Name;
            ViewBag.UserAvatar = account.Employee?.AvatarPath ?? "/images/lion_blue.png";

            // Tạo ViewModel để truyền vào view
            var profileViewModel = new ProfileViewModel
            {
                AccountId = account.Id,
                Username = account.Username,
                Email = account.Email,
                Role = account.Role?.Name,
                Employee = account.Employee,
                Parent = account.Parent
            };

            return View(profileViewModel);
        }

        /// <summary>
        /// POST: /Account/Profile
        /// Cập nhật thông tin cá nhân của user đang đăng nhập
        /// 
        /// Luồng:
        /// 1. Lấy AccountId từ JWT Claims
        /// 2. Validate dữ liệu từ form
        /// 3. Update Account + Employee hoặc Parent
        /// 4. Lưu vào database
        /// 5. Redirect về trang Profile với thông báo thành công
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            // Lấy AccountId từ JWT Claims
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(accountIdClaim, out int accountId))
            {
                return BadRequest("Không thể xác định tài khoản");
            }

            // Query Account từ database
            var account = await _context.Accounts
                .Include(a => a.Role)
                .Include(a => a.Employee)
                .Include(a => a.Parent)
                .FirstOrDefaultAsync(a => a.Id == accountId && a.IsActive);

            if (account == null)
            {
                return NotFound("Tài khoản không tồn tại");
            }

            // Cập nhật Email
            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                account.Email = model.Email;
            }

            // Cập nhật thông tin tùy theo role
            if (account.Role?.Name == "Employee" && account.Employee != null)
            {
                if (!string.IsNullOrWhiteSpace(model.FullName))
                    account.Employee.FullName = model.FullName;
                if (!string.IsNullOrWhiteSpace(model.Phone))
                    account.Employee.Phone = model.Phone;
                if (!string.IsNullOrWhiteSpace(model.Position))
                    account.Employee.Position = model.Position;
            }
            else if (account.Role?.Name == "Parent" && account.Parent != null)
            {
                if (!string.IsNullOrWhiteSpace(model.FirstName))
                    account.Parent.FirstName = model.FirstName;
                if (!string.IsNullOrWhiteSpace(model.LastName))
                    account.Parent.LastName = model.LastName;
                if (!string.IsNullOrWhiteSpace(model.Phone))
                    account.Parent.Phone = model.Phone;
                if (!string.IsNullOrWhiteSpace(model.Address))
                    account.Parent.Address = model.Address;
            }

            // Cập nhật UpdatedAt
            account.UpdatedAt = DateTime.UtcNow;

            try
            {
                _context.Accounts.Update(account);
                await _context.SaveChangesAsync();

                ViewBag.SuccessMessage = "Cập nhật thông tin thành công! ✓";
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Có lỗi khi cập nhật: {ex.Message}";
            }

            // Render lại view với dữ liệu đã cập nhật
            ViewBag.Username = account.Username;
            ViewBag.Email = account.Email;
            ViewBag.Role = account.Role?.Name;
            ViewBag.UserAvatar = account.Employee?.AvatarPath ?? "/images/lion_blue.png";

            model.AccountId = account.Id;
            model.Username = account.Username;
            model.Email = account.Email;
            model.Role = account.Role?.Name;
            model.Employee = account.Employee;
            model.Parent = account.Parent;

            return View(model);
        }

        /// <summary>
        /// GET: /Account/ChangePassword
        /// Hiển thị form đổi mật khẩu
        /// </summary>
        public async Task<IActionResult> ChangePassword()
        {
            // Lấy AccountId từ JWT Claims (NameIdentifier)
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(accountIdClaim, out int accountId))
            {
                return BadRequest("Không thể xác định tài khoản");
            }

            // Query Account từ database
            var account = await _context.Accounts
                .Include(a => a.Role)
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == accountId && a.IsActive);

            if (account == null)
            {
                return NotFound("Tài khoản không tồn tại");
            }

            // Truyền data vào ViewBag
            ViewBag.Username = account.Username;
            ViewBag.Role = account.Role?.Name;
            ViewBag.UserAvatar = account.Employee?.AvatarPath ?? "/images/lion_blue.png";

            // Tạo ViewModel
            var changePasswordViewModel = new ProfileViewModel
            {
                AccountId = account.Id,
                Username = account.Username,
                Role = account.Role?.Name
            };

            return View(changePasswordViewModel);
        }

        /// <summary>
        /// POST: /Account/ChangePassword
        /// Đổi mật khẩu của user đang đăng nhập
        /// 
        /// Luồng:
        /// 1. Lấy AccountId từ JWT Claims
        /// 2. Verify mật khẩu cũ
        /// 3. Validate mật khẩu mới (phải khác mật khẩu cũ, đủ mạnh)
        /// 4. Hash mật khẩu mới và lưu vào database
        /// 5. Trả về thông báo thành công/lỗi
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ProfileViewModel model)
        {
            // Validate - kiểm tra thông tin điền đầy đủ
            if (string.IsNullOrWhiteSpace(model.OldPassword) || 
                string.IsNullOrWhiteSpace(model.NewPassword) || 
                string.IsNullOrWhiteSpace(model.ConfirmPassword))
            {
                ViewBag.PasswordError = "Vui lòng điền đầy đủ thông tin";
                return await LoadProfileViewWithPasswordError();
            }

            // Validate - mật khẩu mới không khớp
            if (model.NewPassword != model.ConfirmPassword)
            {
                ViewBag.PasswordError = "Mật khẩu mới không khớp";
                return await LoadProfileViewWithPasswordError();
            }

            // Validate - mật khẩu mới phải >= 6 ký tự
            if (model.NewPassword.Length < 6)
            {
                ViewBag.PasswordError = "Mật khẩu phải có ít nhất 6 ký tự";
                return await LoadProfileViewWithPasswordError();
            }

            // Lấy AccountId từ JWT Claims
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(accountIdClaim, out int accountId))
            {
                ViewBag.PasswordError = "Không thể xác định tài khoản";
                return await LoadProfileViewWithPasswordError();
            }

            // Query Account từ database
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == accountId && a.IsActive);

            if (account == null)
            {
                ViewBag.PasswordError = "Tài khoản không tồn tại";
                return await LoadProfileViewWithPasswordError();
            }

            // Verify mật khẩu cũ
            if (!BCrypt.Net.BCrypt.Verify(model.OldPassword, account.PasswordHash))
            {
                ViewBag.PasswordError = "Mật khẩu cũ không chính xác";
                return await LoadProfileViewWithPasswordError();
            }

            // Validate - mật khẩu mới không được giống mật khẩu cũ
            if (BCrypt.Net.BCrypt.Verify(model.NewPassword, account.PasswordHash))
            {
                ViewBag.PasswordError = "Mật khẩu mới không được giống với mật khẩu cũ";
                return await LoadProfileViewWithPasswordError();
            }

            try
            {
                // Hash mật khẩu mới
                account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                account.UpdatedAt = DateTime.UtcNow;

                _context.Accounts.Update(account);
                await _context.SaveChangesAsync();

                ViewBag.PasswordSuccess = "Đổi mật khẩu thành công! ✓";
                return await LoadProfileViewForSuccess();
            }
            catch (Exception ex)
            {
                ViewBag.PasswordError = $"Có lỗi khi đổi mật khẩu: {ex.Message}";
                return await LoadProfileViewWithPasswordError();
            }
        }

        /// <summary>
        /// Helper method: Load ChangePasswordView và preserve PasswordError message
        /// </summary>
        private async Task<IActionResult> LoadProfileViewWithPasswordError()
        {
            var passwordError = ViewBag.PasswordError;

            // Lấy AccountId từ JWT Claims
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(accountIdClaim, out int accountId))
            {
                return BadRequest("Không thể xác định tài khoản");
            }

            // Query Account từ database
            var account = await _context.Accounts
                .Include(a => a.Role)
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == accountId && a.IsActive);

            if (account == null)
            {
                return NotFound("Tài khoản không tồn tại");
            }

            // Truyền data vào ViewBag
            ViewBag.Username = account.Username;
            ViewBag.Role = account.Role?.Name;
            ViewBag.UserAvatar = account.Employee?.AvatarPath ?? "/images/lion_blue.png";
            ViewBag.PasswordError = passwordError; // Restore password error

            // Tạo ViewModel để truyền vào view
            var changePasswordViewModel = new ProfileViewModel
            {
                AccountId = account.Id,
                Username = account.Username,
                Role = account.Role?.Name
            };

            return View("ChangePassword", changePasswordViewModel);
        }

        /// <summary>
        /// Helper method: Load ChangePasswordView và preserve PasswordSuccess message
        /// </summary>
        private async Task<IActionResult> LoadProfileViewForSuccess()
        {
            var passwordSuccess = ViewBag.PasswordSuccess;

            // Lấy AccountId từ JWT Claims
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(accountIdClaim, out int accountId))
            {
                return BadRequest("Không thể xác định tài khoản");
            }

            // Query Account từ database
            var account = await _context.Accounts
                .Include(a => a.Role)
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == accountId && a.IsActive);

            if (account == null)
            {
                return NotFound("Tài khoản không tồn tại");
            }

            // Truyền data vào ViewBag
            ViewBag.Username = account.Username;
            ViewBag.Role = account.Role?.Name;
            ViewBag.UserAvatar = account.Employee?.AvatarPath ?? "/images/lion_blue.png";
            ViewBag.PasswordSuccess = passwordSuccess; // Restore password success

            // Tạo ViewModel để truyền vào view
            var changePasswordViewModel = new ProfileViewModel
            {
                AccountId = account.Id,
                Username = account.Username,
                Role = account.Role?.Name
            };

            return View("ChangePassword", changePasswordViewModel);
        }

        /// <summary>
        /// GET: /Auth/AccessDenied
        /// Hiển thị trang không có quyền truy cập (403 Forbidden)
        /// </summary>
        [AllowAnonymous]
        [HttpGet("/Auth/AccessDenied")]
        public async Task<IActionResult> AccessDenied()
        {
            var returnUrl = Request.Query["ReturnUrl"].ToString();
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.Username = User.Identity?.Name ?? "Guest";
            ViewBag.Role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "User";
            ViewBag.UserAvatar = "/images/lion_blue.png";

            if (User.Identity?.IsAuthenticated == true)
            {
                var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(accountIdClaim, out int accountId))
                {
                    var account = await _context.Accounts
                        .Include(a => a.Employee)
                        .FirstOrDefaultAsync(a => a.Id == accountId);
                    if (account != null)
                    {
                        ViewBag.UserAvatar = account.Employee?.AvatarPath ?? "/images/lion_blue.png";
                    }
                }
            }

            return View("~/Views/Auth/AccessDenied.cshtml");
        }
    }

    /// <summary>
    /// ProfileViewModel: Model để truyền dữ liệu từ Controller vào View
    /// </summary>
    public class ProfileViewModel
    {
        public int AccountId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }

        // Dùng cho Employee
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Position { get; set; }

        // Dùng cho Parent
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address { get; set; }

        // Dùng cho đổi mật khẩu
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }

        // Navigation properties
        public Employee Employee { get; set; }
        public Parent Parent { get; set; }
    }
}
