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
    /// </summary>
    [Authorize]
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AccountController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        private void SetProfileViewBag(Account account)
        {
            ViewBag.Username = account.Username;
            ViewBag.Email = account.Email;
            ViewBag.Role = account.Role?.Name;

            string defaultAvatar = account.Role?.Name == "Manager" || account.Role?.Name == "Parent" 
                ? "/images/lion_orange.png" 
                : "/images/lion_blue.png";

            if (account.Role?.Name == "Employee")
            {
                ViewBag.UserAvatar = account.Employee?.AvatarPath ?? defaultAvatar;
            }
            else if (account.Role?.Name == "Parent")
            {
                ViewBag.UserAvatar = account.Parent?.AvatarPath ?? defaultAvatar;
            }
            else
            {
                // Manager might not have Employee record or AvatarPath in some cases, 
                // but usually Manager is also an Employee in this system.
                ViewBag.UserAvatar = account.Employee?.AvatarPath ?? defaultAvatar;
            }
        }

        public async Task<IActionResult> Profile()
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(accountIdClaim, out int accountId))
            {
                return BadRequest("Không thể xác định tài khoản");
            }

            var account = await _context.Accounts
                .Include(a => a.Role)
                .Include(a => a.Employee)
                .Include(a => a.Parent)
                .FirstOrDefaultAsync(a => a.Id == accountId && a.IsActive);

            if (account == null)
            {
                return NotFound("Tài khoản không tồn tại");
            }

            SetProfileViewBag(account);

            var profileViewModel = new ProfileViewModel
            {
                AccountId = account.Id,
                Username = account.Username,
                Email = account.Email,
                Role = account.Role?.Name,
                FullName = account.Employee?.FullName,
                Phone = account.Employee?.Phone ?? account.Parent?.Phone,
                Position = account.Employee?.Position,
                FirstName = account.Parent?.FirstName,
                LastName = account.Parent?.LastName,
                Address = account.Parent?.Address,
                Employee = account.Employee,
                Parent = account.Parent
            };

            return View(profileViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(accountIdClaim, out int accountId))
            {
                return BadRequest("Không thể xác định tài khoản");
            }

            var account = await _context.Accounts
                .Include(a => a.Role)
                .Include(a => a.Employee)
                .Include(a => a.Parent)
                .FirstOrDefaultAsync(a => a.Id == accountId && a.IsActive);

            if (account == null)
            {
                return NotFound("Tài khoản không tồn tại");
            }

            // Handle Avatar Upload
            if (model.AvatarFile != null && model.AvatarFile.Length > 0)
            {
                try
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatars");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.AvatarFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.AvatarFile.CopyToAsync(fileStream);
                    }

                    string avatarRelativePath = "/uploads/avatars/" + uniqueFileName;

                    if (account.Employee != null)
                    {
                        account.Employee.AvatarPath = avatarRelativePath;
                    }
                    if (account.Parent != null)
                    {
                        account.Parent.AvatarPath = avatarRelativePath;
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = $"Lỗi khi tải ảnh lên: {ex.Message}";
                }
            }

            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                account.Email = model.Email;
            }

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

            SetProfileViewBag(account);

            model.AccountId = account.Id;
            model.Username = account.Username;
            model.Email = account.Email;
            model.Role = account.Role?.Name;
            model.Employee = account.Employee;
            model.Parent = account.Parent;

            return View(model);
        }

        public async Task<IActionResult> ChangePassword()
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(accountIdClaim, out int accountId))
            {
                return BadRequest("Không thể xác định tài khoản");
            }

            var account = await _context.Accounts
                .Include(a => a.Role)
                .Include(a => a.Employee)
                .Include(a => a.Parent)
                .FirstOrDefaultAsync(a => a.Id == accountId && a.IsActive);

            if (account == null)
            {
                return NotFound("Tài khoản không tồn tại");
            }

            SetProfileViewBag(account);

            var changePasswordViewModel = new ProfileViewModel
            {
                AccountId = account.Id,
                Username = account.Username,
                Role = account.Role?.Name
            };

            return View(changePasswordViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ProfileViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.OldPassword) || 
                string.IsNullOrWhiteSpace(model.NewPassword) || 
                string.IsNullOrWhiteSpace(model.ConfirmPassword))
            {
                ViewBag.PasswordError = "Vui lòng điền đầy đủ thông tin";
                return await LoadProfileViewWithPasswordError();
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                ViewBag.PasswordError = "Mật khẩu mới không khớp";
                return await LoadProfileViewWithPasswordError();
            }

            if (model.NewPassword.Length < 6)
            {
                ViewBag.PasswordError = "Mật khẩu phải có ít nhất 6 ký tự";
                return await LoadProfileViewWithPasswordError();
            }

            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(accountIdClaim, out int accountId))
            {
                ViewBag.PasswordError = "Không thể xác định tài khoản";
                return await LoadProfileViewWithPasswordError();
            }

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == accountId && a.IsActive);

            if (account == null)
            {
                ViewBag.PasswordError = "Tài khoản không tồn tại";
                return await LoadProfileViewWithPasswordError();
            }

            if (!BCrypt.Net.BCrypt.Verify(model.OldPassword, account.PasswordHash))
            {
                ViewBag.PasswordError = "Mật khẩu cũ không chính xác";
                return await LoadProfileViewWithPasswordError();
            }

            if (BCrypt.Net.BCrypt.Verify(model.NewPassword, account.PasswordHash))
            {
                ViewBag.PasswordError = "Mật khẩu mới không được giống với mật khẩu cũ";
                return await LoadProfileViewWithPasswordError();
            }

            try
            {
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

        private async Task<IActionResult> LoadProfileViewWithPasswordError()
        {
            var passwordError = ViewBag.PasswordError;

            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(accountIdClaim, out int accountId))
            {
                return BadRequest("Không thể xác định tài khoản");
            }

            var account = await _context.Accounts
                .Include(a => a.Role)
                .Include(a => a.Employee)
                .Include(a => a.Parent)
                .FirstOrDefaultAsync(a => a.Id == accountId && a.IsActive);

            if (account == null)
            {
                return NotFound("Tài khoản không tồn tại");
            }

            SetProfileViewBag(account);
            ViewBag.PasswordError = passwordError;

            var changePasswordViewModel = new ProfileViewModel
            {
                AccountId = account.Id,
                Username = account.Username,
                Role = account.Role?.Name
            };

            return View("ChangePassword", changePasswordViewModel);
        }

        private async Task<IActionResult> LoadProfileViewForSuccess()
        {
            var passwordSuccess = ViewBag.PasswordSuccess;

            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(accountIdClaim, out int accountId))
            {
                return BadRequest("Không thể xác định tài khoản");
            }

            var account = await _context.Accounts
                .Include(a => a.Role)
                .Include(a => a.Employee)
                .Include(a => a.Parent)
                .FirstOrDefaultAsync(a => a.Id == accountId && a.IsActive);

            if (account == null)
            {
                return NotFound("Tài khoản không tồn tại");
            }

            SetProfileViewBag(account);
            ViewBag.PasswordSuccess = passwordSuccess;

            var changePasswordViewModel = new ProfileViewModel
            {
                AccountId = account.Id,
                Username = account.Username,
                Role = account.Role?.Name
            };

            return View("ChangePassword", changePasswordViewModel);
        }

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
                        .Include(a => a.Role)
                        .Include(a => a.Employee)
                        .Include(a => a.Parent)
                        .FirstOrDefaultAsync(a => a.Id == accountId);
                    if (account != null)
                    {
                        SetProfileViewBag(account);
                    }
                }
            }

            return View("~/Views/Auth/AccessDenied.cshtml");
        }
    }

    public class ProfileViewModel
    {
        public int AccountId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }

        public IFormFile? AvatarFile { get; set; }

        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Position { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address { get; set; }

        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }

        public Employee Employee { get; set; }
        public Parent Parent { get; set; }
    }
}
