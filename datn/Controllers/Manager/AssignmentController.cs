using datn.Data;
using datn.Models;
using datn.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace datn.Controllers.Manager
{
    [Authorize(Roles = "Manager")]
    [Route("Manager")]
    public class AssignmentController : BaseController
    {
        public AssignmentController(AppDbContext context) : base(context) { }

        // ============ ASSIGNMENT API ============

        [HttpGet("Api/Assignments")]
        public async Task<IActionResult> GetAssignments()
        {
            try
            {
                var assignments = await _context.Assignments
                    .Include(a => a.Employee)
                    .Include(a => a.Class)
                    .OrderByDescending(a => a.StartDate)
                    .ToListAsync();

                var result = assignments.Select(a => new
                {
                    employeeId = a.EmployeeId,
                    employeeName = a.Employee?.FullName ?? "N/A",
                    classId = a.ClassId,
                    className = a.Class?.Name ?? "N/A",
                    startDate = a.StartDate.ToString("yyyy-MM-dd"),
                    endDate = a.EndDate?.ToString("yyyy-MM-dd") ?? "",
                    roleInClass = TeacherRoleDisplay.ToDisplayName(a.RoleInClass)
                }).ToList();

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("Api/Assignment")]
        public async Task<IActionResult> CreateAssignment([FromBody] CreateAssignmentViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ" });

                if (model.EmployeeId <= 0 || model.ClassId <= 0)
                    return Json(new { success = false, message = "Nhân viên và lớp học là bắt buộc." });

                if (string.IsNullOrWhiteSpace(model.StartDate))
                    return Json(new { success = false, message = "Ngày bắt đầu không được để trống." });

                if (!DateOnly.TryParse(model.StartDate, out var startDate))
                    return Json(new { success = false, message = "Ngày bắt đầu không hợp lệ." });

                DateOnly? endDate = null;
                if (!string.IsNullOrWhiteSpace(model.EndDate))
                {
                    if (!DateOnly.TryParse(model.EndDate, out var parsedEndDate))
                        return Json(new { success = false, message = "Ngày kết thúc không hợp lệ." });
                    endDate = parsedEndDate;
                    if (endDate < startDate)
                        return Json(new { success = false, message = "Ngày kết thúc phải sau hoặc trùng ngày bắt đầu." });
                }

                if (string.IsNullOrWhiteSpace(model.RoleInClass))
                    return Json(new { success = false, message = "Vai trò trong lớp không được để trống." });

                if (!await _context.Employees.AnyAsync(e => e.Id == model.EmployeeId))
                    return Json(new { success = false, message = "Giáo viên không tồn tại." });

                if (!await _context.Classes.AnyAsync(c => c.Id == model.ClassId))
                    return Json(new { success = false, message = "Lớp học không tồn tại." });

                var exists = await _context.Assignments.AnyAsync(a =>
                    a.EmployeeId == model.EmployeeId && a.ClassId == model.ClassId && a.StartDate == startDate);
                if (exists) return Json(new { success = false, message = "Phân công này đã tồn tại" });

                var overlappingCount = await CountOverlappingAssignmentsAsync(model.ClassId, startDate, endDate);
                if (overlappingCount >= 2)
                    return Json(new { success = false, message = "Một lớp chỉ được phân công tối đa 2 giáo viên phụ trách trong cùng thời gian." });

                var assignment = new Assignment
                {
                    EmployeeId = model.EmployeeId,
                    ClassId = model.ClassId,
                    StartDate = startDate,
                    EndDate = endDate,
                    RoleInClass = model.RoleInClass.Trim()
                };
                _context.Assignments.Add(assignment);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Phân công thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("Api/Assignment")]
        public async Task<IActionResult> UpdateAssignment([FromBody] CreateAssignmentViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ" });

                if (model.EmployeeId <= 0 || model.ClassId <= 0)
                    return Json(new { success = false, message = "Nhân viên và lớp học là bắt buộc." });

                if (string.IsNullOrWhiteSpace(model.StartDate))
                    return Json(new { success = false, message = "Ngày bắt đầu không được để trống." });

                if (!DateOnly.TryParse(model.StartDate, out var newStartDate))
                    return Json(new { success = false, message = "Ngày bắt đầu không hợp lệ." });

                var oldStartDateText = string.IsNullOrWhiteSpace(model.OldStartDate) ? model.StartDate : model.OldStartDate;
                if (!DateOnly.TryParse(oldStartDateText, out var oldSDate))
                    return Json(new { success = false, message = "Ngày bắt đầu cũ không hợp lệ." });

                var oldEmpId = model.OldEmployeeId ?? model.EmployeeId;
                var oldClsId = model.OldClassId ?? model.ClassId;

                var assignment = await _context.Assignments.FirstOrDefaultAsync(a =>
                    a.EmployeeId == oldEmpId && a.ClassId == oldClsId && a.StartDate == oldSDate);

                if (assignment == null) return Json(new { success = false, message = "Không tìm thấy phân công để cập nhật" });

                DateOnly? newEndDate = null;
                if (!string.IsNullOrWhiteSpace(model.EndDate))
                {
                    if (!DateOnly.TryParse(model.EndDate, out var parsedEndDate))
                        return Json(new { success = false, message = "Ngày kết thúc không hợp lệ." });
                    newEndDate = parsedEndDate;
                    if (newEndDate < newStartDate)
                        return Json(new { success = false, message = "Ngày kết thúc phải sau hoặc trùng ngày bắt đầu." });
                }

                if (string.IsNullOrWhiteSpace(model.RoleInClass))
                    return Json(new { success = false, message = "Vai trò trong lớp không được để trống." });

                if (!await _context.Employees.AnyAsync(e => e.Id == model.EmployeeId))
                    return Json(new { success = false, message = "Giáo viên không tồn tại." });

                if (!await _context.Classes.AnyAsync(c => c.Id == model.ClassId))
                    return Json(new { success = false, message = "Lớp học không tồn tại." });

                var isSameIdentity = oldEmpId == model.EmployeeId && oldClsId == model.ClassId && oldSDate == newStartDate;

                if (!isSameIdentity)
                {
                    var duplicate = await _context.Assignments.AnyAsync(a =>
                        a.EmployeeId == model.EmployeeId && a.ClassId == model.ClassId && a.StartDate == newStartDate);
                    if (duplicate) return Json(new { success = false, message = "Phân công mới đã tồn tại" });
                }

                var overlappingCount = await CountOverlappingAssignmentsAsync(
                    model.ClassId,
                    newStartDate,
                    newEndDate,
                    oldEmpId,
                    oldClsId,
                    oldSDate);

                if (overlappingCount >= 2)
                    return Json(new { success = false, message = "Một lớp chỉ được phân công tối đa 2 giáo viên phụ trách trong cùng thời gian." });

                if (!isSameIdentity)
                {
                    _context.Assignments.Remove(assignment);
                    await _context.SaveChangesAsync();

                    var newAssignment = new Assignment
                    {
                        EmployeeId = model.EmployeeId,
                        ClassId = model.ClassId,
                        StartDate = newStartDate,
                        EndDate = newEndDate,
                        RoleInClass = model.RoleInClass.Trim()
                    };
                    _context.Assignments.Add(newAssignment);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    assignment.EndDate = newEndDate;
                    assignment.RoleInClass = model.RoleInClass.Trim();
                    _context.Assignments.Update(assignment);
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true, message = "Cập nhật phân công thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("Api/Assignment")]
        public async Task<IActionResult> DeleteAssignment(int employeeId, int classId, string startDate)
        {
            var sDate = DateOnly.Parse(startDate);
            var assignment = await _context.Assignments
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.ClassId == classId && a.StartDate == sDate);

            if (assignment == null) return Json(new { success = false, message = "Không tìm thấy phân công" });

            assignment.IsActive = false;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã ẩn phân công giảng dạy thành công." });
        }

        [HttpPost("Api/Assignment/Reactivate")]
        public async Task<IActionResult> ReactivateAssignment(int employeeId, int classId, string startDate)
        {
            var sDate = DateOnly.Parse(startDate);
            var assignment = await _context.Assignments.IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.ClassId == classId && a.StartDate == sDate);

            if (assignment == null) return Json(new { success = false, message = "Không tìm thấy." });

            var overlappingCount = await CountOverlappingAssignmentsAsync(
                assignment.ClassId,
                assignment.StartDate,
                assignment.EndDate,
                assignment.EmployeeId,
                assignment.ClassId,
                assignment.StartDate);

            if (overlappingCount >= 2)
                return Json(new { success = false, message = "Một lớp chỉ được phân công tối đa 2 giáo viên phụ trách trong cùng thời gian." });

            assignment.IsActive = true;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã khôi phục phân công giảng dạy thành công." });
        }

        // ============ PRIVATE HELPERS ============

        private async Task<int> CountOverlappingAssignmentsAsync(
            int classId,
            DateOnly startDate,
            DateOnly? endDate,
            int? excludeEmployeeId = null,
            int? excludeClassId = null,
            DateOnly? excludeStartDate = null)
        {
            var rangeEnd = endDate ?? DateOnly.MaxValue;
            var query = _context.Assignments.Where(a =>
                a.ClassId == classId &&
                a.IsActive &&
                a.StartDate <= rangeEnd &&
                (a.EndDate == null || a.EndDate >= startDate));

            if (excludeEmployeeId.HasValue && excludeClassId.HasValue && excludeStartDate.HasValue)
            {
                query = query.Where(a =>
                    !(a.EmployeeId == excludeEmployeeId.Value &&
                      a.ClassId == excludeClassId.Value &&
                      a.StartDate == excludeStartDate.Value));
            }

            return await query.CountAsync();
        }
    }
}


