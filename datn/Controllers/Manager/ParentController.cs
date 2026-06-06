using datn.Data;
using datn.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace datn.Controllers.Manager
{
    [Authorize(Roles = "Manager")]
    [Route("Manager")]
    public class ParentController : BaseController
    {
        private readonly IParentService _parentService;

        public ParentController(AppDbContext context, IParentService parentService) : base(context)
        {
            _parentService = parentService;
        }

        // ============ PARENT MANAGEMENT ============

        [HttpGet("Api/Parents")]
        public async Task<IActionResult> GetParents(string search = "", int page = 1, int pageSize = 10, bool showInactive = false)
        {
            try
            {
                var query = _context.Parents.AsQueryable();
                
                if (showInactive)
                {
                    query = query.IgnoreQueryFilters().Where(p => !p.IsActive);
                }

                query = query
                    .Include(p => p.Account)
                    .Include(p => p.ParentStudents)
                        .ThenInclude(ps => ps.Student)
                            .ThenInclude(s => s.Class);

                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    query = query.Where(p => 
                        p.FirstName.ToLower().Contains(search) || 
                        p.LastName.ToLower().Contains(search) || 
                        (p.Phone != null && p.Phone.Contains(search)));
                }

                var total = await query.CountAsync();
                var data = await query
                    .OrderByDescending(p => p.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var result = data.Select(p => new {
                        id = p.Id,
                        username = p.Account?.Username ?? "N/A",
                        email = p.Account?.Email ?? "N/A",
                        fullName = p.LastName + " " + p.FirstName,
                        gender = p.Gender,
                        dateOfBirth = p.DateOfBirth?.ToString("yyyy-MM-dd"),
                        phone = p.Phone ?? "N/A",
                        address = p.Address ?? "N/A",
                        avatarPath = p.AvatarPath ?? "/images/lion_orange.png",
                        isActive = p.IsActive,
                        createdAt = p.Account?.CreatedAt ?? DateTime.MinValue,
                        updatedAt = p.Account?.UpdatedAt ?? DateTime.MinValue,
                        childrenCount = p.ParentStudents.Count,
                        children = p.ParentStudents.Select(ps => new {
                            id = ps.StudentId,
                            fullName = ps.Student.LastName + " " + ps.Student.FirstName,
                            relationship = ps.Relationship ?? "Phụ huynh",
                            className = ps.Student.Class != null ? ps.Student.Class.Name : "N/A"
                        })
                    });

                return Json(new { success = true, data = result, total });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("Api/Parent/{id:int}")]
        public async Task<IActionResult> GetParent(int id)
        {
            try
            {
                var p = await _context.Parents
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (p != null)
                {
                    p.Account = await _context.Accounts.IgnoreQueryFilters()
                        .FirstOrDefaultAsync(a => a.Id == p.AccountId);
                    p.ParentStudents = await _context.ParentStudents
                        .Include(ps => ps.Student)
                        .Where(ps => ps.ParentId == p.Id)
                        .ToListAsync();
                }

                if (p == null) return Json(new { success = false, message = "Không tìm thấy" });

                var result = new {
                    id = p.Id,
                    username = p.Account.Username,
                    email = p.Account.Email,
                    firstName = p.FirstName,
                    lastName = p.LastName,
                    gender = p.Gender,
                    dateOfBirth = p.DateOfBirth?.ToString("yyyy-MM-dd"),
                    phone = p.Phone,
                    address = p.Address,
                    avatarPath = p.AvatarPath ?? "/images/lion_orange.png",
                    isActive = p.IsActive,
                    createdAt = p.Account.CreatedAt.ToString("dd/MM/yyyy"),
                    children = p.ParentStudents.Select(ps => new {
                        id = ps.StudentId,
                        fullName = ps.Student.LastName + " " + ps.Student.FirstName,
                        relationship = ps.Relationship
                    }).ToList()
                };

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("Api/Parent")]
        public async Task<IActionResult> CreateParent([FromForm] datn.DTOs.CreateParentDto model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = errors });
            }

            try
            {
                if (await _parentService.IsEmailOrUsernameExists(model.Email, model.Username))
                {
                    return Json(new { success = false, message = "Email hoặc Tên đăng nhập đã tồn tại" });
                }

                await _parentService.CreateParentAsync(model);
                return Json(new { success = true, message = "Tạo tài khoản phụ huynh thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("Api/Parent/{id:int}")]
        public async Task<IActionResult> UpdateParent(int id, [FromForm] datn.DTOs.CreateParentDto model)
        {
            // Khi update, chúng ta không cho phép Manager sửa mật khẩu ở đây
            // nên xóa lỗi validation của Password nếu có
            ModelState.Remove("Password");

            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = errors });
            }

            try
            {
                if (await _parentService.IsEmailOrUsernameExists(model.Email, model.Username, id))
                {
                    return Json(new { success = false, message = "Email hoặc Tên đăng nhập đã tồn tại" });
                }

                var parent = await _parentService.UpdateParentAsync(id, model);
                if (parent == null) return Json(new { success = false, message = "Không tìm thấy" });

                return Json(new { success = true, message = "Cập nhật thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("Api/Parent/{id:int}")]
        public async Task<IActionResult> DeleteParent(int id)
        {
            var success = await _parentService.DeleteParentAsync(id);
            return Json(new { success, message = success ? "Vô hiệu hóa thành công" : "Lỗi vô hiệu hóa" });
        }

        [HttpPost("Api/Parent/Reactivate/{id:int}")]
        public async Task<IActionResult> ReactivateParent(int id)
        {
            var parent = await _context.Parents
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (parent != null)
            {
                parent.Account = await _context.Accounts.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(a => a.Id == parent.AccountId);
            }
            if (parent == null) return Json(new { success = false, message = "Không tìm thấy phụ huynh" });

            parent.IsActive = true;
            if (parent.Account != null)
            {
                parent.Account.IsActive = true;
            }
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Kích hoạt tài khoản thành công" });
        }

        [HttpPost("Api/Parent/LinkStudent")]
        public async Task<IActionResult> LinkStudent([FromForm] int parentId, [FromForm] int studentId, [FromForm] string? relationship)
        {
            if (parentId <= 0 || studentId <= 0)
            {
                return Json(new { success = false, message = $"Du lieu khong hop le (parentId={parentId}, studentId={studentId})." });
            }

            if (!await _context.Parents.AnyAsync(p => p.Id == parentId))
            {
                return Json(new { success = false, message = "Khong tim thay phu huynh." });
            }

            if (!await _context.Students.AnyAsync(s => s.Id == studentId))
            {
                return Json(new { success = false, message = "Khong tim thay hoc sinh." });
            }

            try
            {
                var success = await _parentService.LinkStudentAsync(parentId, studentId, relationship ?? "");
            return Json(new { success, message = success ? "Liên kết thành công" : "Lỗi khi liên kết" });
            }
            catch (DbUpdateException ex)
            {
                return Json(new { success = false, message = $"Loi CSDL khi lien ket: {ex.GetBaseException().Message}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Loi khi lien ket: {ex.Message}" });
            }
        }

        [HttpDelete("Api/Parent/UnlinkStudent")]
        public async Task<IActionResult> UnlinkStudent(int parentId, int studentId)
        {
            var success = await _parentService.UnlinkStudentAsync(parentId, studentId);
            return Json(new { success, message = success ? "Hủy liên kết thành công" : "Lỗi khi hủy liên kết" });
        }
    }
}
