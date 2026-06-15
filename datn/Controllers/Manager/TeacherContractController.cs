using datn.Data;
using datn.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace datn.Controllers.Manager
{
    [Authorize(Roles = "Manager")]
    [Route("Manager")]
    public class TeacherContractController : BaseController
    {
        private const long MaxContractFileSize = 10 * 1024 * 1024;
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png"
        };

        public TeacherContractController(AppDbContext context) : base(context) { }

        [HttpGet("Api/TeacherContracts")]
        public async Task<IActionResult> GetContracts(string? status = null, int? employeeId = null, int? expiringWithinDays = null)
        {
            await MarkExpiredContractsAsync();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var soon = today.AddDays(30);

            var query = _context.TeacherContracts
                .Include(c => c.Employee).ThenInclude(e => e.Account)
                .AsQueryable();

            if (employeeId.HasValue)
                query = query.Where(c => c.EmployeeId == employeeId.Value);

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<TeacherContractStatus>(status, true, out var parsedStatus))
                query = query.Where(c => c.Status == parsedStatus);

            if (expiringWithinDays.HasValue)
            {
                var until = today.AddDays(expiringWithinDays.Value);
                query = query.Where(c => c.Status == TeacherContractStatus.Active
                    && c.ExpiryDate.HasValue
                    && c.ExpiryDate.Value >= today
                    && c.ExpiryDate.Value <= until);
            }

            var contracts = await query
                .OrderByDescending(c => c.EffectiveDate)
                .ThenByDescending(c => c.Id)
                .ToListAsync();

            var data = contracts.Select(c => new
                {
                    id = c.Id,
                    employeeId = c.EmployeeId,
                    employeeName = c.Employee.LastName + " " + c.Employee.FirstName,
                    contractNumber = c.ContractNumber,
                    contractType = c.ContractType,
                    signedDate = c.SignedDate.ToString("yyyy-MM-dd"),
                    effectiveDate = c.EffectiveDate.ToString("yyyy-MM-dd"),
                    expiryDate = c.ExpiryDate.HasValue ? c.ExpiryDate.Value.ToString("yyyy-MM-dd") : null,
                    agreedSalary = c.AgreedSalary,
                    workPosition = c.WorkPosition,
                    status = c.Status.ToString(),
                    hasFile = c.StoredFileName != null,
                    isExpiringSoon = c.Status == TeacherContractStatus.Active
                        && c.ExpiryDate.HasValue
                        && c.ExpiryDate.Value >= today
                        && c.ExpiryDate.Value <= soon
                })
                .ToList();

            return Json(new { success = true, data });
        }

        [HttpGet("Api/TeacherContractAlerts")]
        public async Task<IActionResult> GetContractAlerts()
        {
            await MarkExpiredContractsAsync();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var soon = today.AddDays(30);

            var activeContracts = await _context.TeacherContracts.CountAsync(c => c.Status == TeacherContractStatus.Active);
            var expiringSoon = await _context.TeacherContracts.CountAsync(c => c.Status == TeacherContractStatus.Active
                && c.ExpiryDate.HasValue
                && c.ExpiryDate.Value >= today
                && c.ExpiryDate.Value <= soon);
            var teachersWithoutContracts = await _context.Employees
                .Include(e => e.Account).ThenInclude(a => a.Role)
                .CountAsync(e => e.Account.Role.Name == "Employee" && !e.TeacherContracts.Any());

            return Json(new { success = true, data = new { activeContracts, expiringSoon, teachersWithoutContracts } });
        }

        [HttpGet("Api/Teacher/{employeeId:int}/Contracts")]
        public async Task<IActionResult> GetTeacherContracts(int employeeId)
        {
            await MarkExpiredContractsAsync();
            if (!await IsTeacherAsync(employeeId))
                return Json(new { success = false, message = "Không tìm thấy giáo viên" });

            var contracts = await _context.TeacherContracts
                .Where(c => c.EmployeeId == employeeId)
                .OrderByDescending(c => c.EffectiveDate)
                .ThenByDescending(c => c.Id)
                .ToListAsync();

            return Json(new { success = true, data = contracts.Select(ToDto) });
        }

        [HttpGet("Api/TeacherContract/{id:int}")]
        public async Task<IActionResult> GetContract(int id)
        {
            await MarkExpiredContractsAsync();
            var contract = await _context.TeacherContracts
                .Include(c => c.Employee)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract == null)
                return Json(new { success = false, message = "Không tìm thấy hợp đồng." });

            return Json(new { success = true, data = ToDto(contract) });
        }

        [HttpPost("Api/Teacher/{employeeId:int}/Contracts")]
        public async Task<IActionResult> CreateContract(int employeeId, [FromForm] SaveTeacherContractViewModel model)
        {
            if (!await IsTeacherAsync(employeeId))
                return Json(new { success = false, message = "Không tìm thấy giáo viên." });

            var validation = await ValidateContractAsync(model, null);
            if (validation != null)
                return Json(new { success = false, message = validation });

            var contract = new TeacherContract
            {
                EmployeeId = employeeId
            };
            ApplyModel(contract, model);
            await SaveContractFileAsync(contract, model.File);

            using var tx = await _context.Database.BeginTransactionAsync();
            _context.TeacherContracts.Add(contract);
            if (contract.Status == TeacherContractStatus.Active)
                await ActivateContractInternalAsync(contract);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return Json(new { success = true, message = "Da tao hop dong thanh cong.", data = ToDto(contract) });
        }

        [HttpPut("Api/TeacherContract/{id:int}")]
        public async Task<IActionResult> UpdateContract(int id, [FromForm] SaveTeacherContractViewModel model)
        {
            var contract = await _context.TeacherContracts.Include(c => c.Employee).FirstOrDefaultAsync(c => c.Id == id);
            if (contract == null)
                return Json(new { success = false, message = "Không tìm thấy hợp đồng." });

            if (contract.Status != TeacherContractStatus.Draft)
                return Json(new { success = false, message = "Chỉ được sửa hợp đồng ở trạng thái Draft." });

            var validation = await ValidateContractAsync(model, id);
            if (validation != null)
                return Json(new { success = false, message = validation });

            using var tx = await _context.Database.BeginTransactionAsync();
            ApplyModel(contract, model);
            await SaveContractFileAsync(contract, model.File);
            if (contract.Status == TeacherContractStatus.Active)
                await ActivateContractInternalAsync(contract);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return Json(new { success = true, message = "Đã cập nhật hợp đồng thành công.", data = ToDto(contract) });
        }

        [HttpPost("Api/TeacherContract/{id:int}/Activate")]
        public async Task<IActionResult> ActivateContract(int id)
        {
            var contract = await _context.TeacherContracts.Include(c => c.Employee).FirstOrDefaultAsync(c => c.Id == id);
            if (contract == null)
                return Json(new { success = false, message = "Không tìm thấy hợp đồng." });

            if (contract.Status != TeacherContractStatus.Draft)
                return Json(new { success = false, message = "Chỉ được kích hoạt hợp đồng ở trạng thái Draft." });

            using var tx = await _context.Database.BeginTransactionAsync();
            await ActivateContractInternalAsync(contract);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return Json(new { success = true, message = "Đã kích hoạt hợp đồng.", data = ToDto(contract) });
        }

        [HttpPost("Api/TeacherContract/{id:int}/Terminate")]
        public async Task<IActionResult> TerminateContract(int id, [FromBody] TerminateTeacherContractViewModel model)
        {
            var contract = await _context.TeacherContracts.FindAsync(id);
            if (contract == null)
                return Json(new { success = false, message = "Không tìm thấy hợp đồng." });

            if (!DateOnly.TryParse(model.TerminationDate, out var terminationDate))
                return Json(new { success = false, message = "Ngày chấm dứt không hợp lệ." });

            if (string.IsNullOrWhiteSpace(model.TerminationReason))
                return Json(new { success = false, message = "Lý do chấm dứt là bắt buộc." });

            contract.Status = TeacherContractStatus.Terminated;
            contract.TerminationDate = terminationDate;
            contract.TerminationReason = model.TerminationReason.Trim();
            contract.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã chấm dứt hợp đồng.", data = ToDto(contract) });
        }

        [HttpPost("Api/TeacherContract/{id:int}/Cancel")]
        public async Task<IActionResult> CancelContract(int id)
        {
            var contract = await _context.TeacherContracts.FindAsync(id);
            if (contract == null)
                return Json(new { success = false, message = "Không tìm thấy hợp đồng." });

            if (contract.Status != TeacherContractStatus.Draft)
                return Json(new { success = false, message = "Chỉ được hủy hợp đồng ở trạng thái Draft." });

            contract.Status = TeacherContractStatus.Cancelled;
            contract.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã hủy hợp đồng.", data = ToDto(contract) });
        }

        [HttpDelete("Api/TeacherContract/{id:int}")]
        public async Task<IActionResult> DeleteContract(int id)
        {
            var contract = await _context.TeacherContracts.FindAsync(id);
            if (contract == null)
                return Json(new { success = false, message = "Không tìm thấy hợp đồng." });

            if (contract.Status == TeacherContractStatus.Active)
                return Json(new { success = false, message = "Không thể xóa hợp đồng đang hiệu lực." });

            DeleteStoredFile(contract.StoredFileName);
            _context.TeacherContracts.Remove(contract);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa hợp đồng." });
        }

        [HttpGet("Api/TeacherContract/{id:int}/File")]
        public async Task<IActionResult> DownloadContractFile(int id)
        {
            var contract = await _context.TeacherContracts.FindAsync(id);
            if (contract == null || string.IsNullOrWhiteSpace(contract.StoredFileName))
                return NotFound();

            var path = Path.Combine(GetContractFolder(), contract.StoredFileName);
            if (!System.IO.File.Exists(path))
                return NotFound();

            var contentType = contract.ContentType ?? "application/octet-stream";
            var fileName = contract.OriginalFileName ?? contract.StoredFileName;
            return PhysicalFile(path, contentType, fileName);
        }

        private async Task<string?> ValidateContractAsync(SaveTeacherContractViewModel model, int? contractId)
        {
            if (string.IsNullOrWhiteSpace(model.ContractNumber))
                return "So hop dong la bat buoc.";
            if (string.IsNullOrWhiteSpace(model.ContractType))
                return "Loai hop dong la bat buoc.";
            if (!DateOnly.TryParse(model.SignedDate, out _))
                return "Ngay ky khong hop le.";
            if (!DateOnly.TryParse(model.EffectiveDate, out var effectiveDate))
                return "Ngay hieu luc khong hop le.";
            if (!string.IsNullOrWhiteSpace(model.ExpiryDate)
                && DateOnly.TryParse(model.ExpiryDate, out var expiryDate)
                && expiryDate < effectiveDate)
                return "Ngay het han khong duoc truoc ngay hieu luc.";
            if (!string.IsNullOrWhiteSpace(model.ExpiryDate) && !DateOnly.TryParse(model.ExpiryDate, out _))
                return "Ngay het han khong hop le.";

            var normalizedNumber = model.ContractNumber.Trim();
            var duplicate = await _context.TeacherContracts
                .AnyAsync(c => c.Id != (contractId ?? 0) && c.ContractNumber == normalizedNumber);
            if (duplicate)
                return "So hop dong da ton tai.";

            if (model.File != null)
            {
                var ext = Path.GetExtension(model.File.FileName);
                if (!AllowedExtensions.Contains(ext))
                    return "File hop dong chi nhan PDF, DOC, DOCX, JPG, JPEG hoac PNG.";
                if (model.File.Length > MaxContractFileSize)
                    return "File hop dong khong duoc vuot qua 10MB.";
            }

            return null;
        }

        private void ApplyModel(TeacherContract contract, SaveTeacherContractViewModel model)
        {
            contract.ContractNumber = model.ContractNumber.Trim();
            contract.ContractType = model.ContractType.Trim();
            contract.SignedDate = DateOnly.Parse(model.SignedDate);
            contract.EffectiveDate = DateOnly.Parse(model.EffectiveDate);
            contract.ExpiryDate = string.IsNullOrWhiteSpace(model.ExpiryDate) ? null : DateOnly.Parse(model.ExpiryDate);
            contract.AgreedSalary = model.AgreedSalary;
            contract.WorkPosition = model.WorkPosition?.Trim();
            contract.WorkLocation = model.WorkLocation?.Trim();
            contract.WorkingHours = model.WorkingHours?.Trim();
            contract.Status = model.Status;
            contract.Note = model.Note?.Trim();
            contract.UpdatedAtUtc = DateTime.UtcNow;
        }

        private async Task ActivateContractInternalAsync(TeacherContract contract)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var activeContracts = await _context.TeacherContracts
                .Where(c => c.EmployeeId == contract.EmployeeId
                    && c.Id != contract.Id
                    && c.Status == TeacherContractStatus.Active)
                .ToListAsync();

            foreach (var active in activeContracts)
            {
                active.Status = TeacherContractStatus.Terminated;
                active.TerminationDate = today;
                active.TerminationReason = "Replaced by contract " + contract.ContractNumber;
                active.UpdatedAtUtc = DateTime.UtcNow;
            }

            contract.Status = TeacherContractStatus.Active;
            contract.TerminationDate = null;
            contract.TerminationReason = null;
            contract.UpdatedAtUtc = DateTime.UtcNow;

            if (contract.AgreedSalary.HasValue)
            {
                var employee = contract.Employee ?? await _context.Employees.FindAsync(contract.EmployeeId);
                if (employee != null)
                    employee.BaseSalary = contract.AgreedSalary;
            }
        }

        private async Task MarkExpiredContractsAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var expired = await _context.TeacherContracts
                .Where(c => c.Status == TeacherContractStatus.Active
                    && c.ExpiryDate.HasValue
                    && c.ExpiryDate.Value < today)
                .ToListAsync();

            if (expired.Count == 0)
                return;

            foreach (var contract in expired)
            {
                contract.Status = TeacherContractStatus.Expired;
                contract.UpdatedAtUtc = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        private async Task<bool> IsTeacherAsync(int employeeId)
        {
            return await _context.Employees
                .IgnoreQueryFilters()
                .Include(e => e.Account).ThenInclude(a => a.Role)
                .AnyAsync(e => e.Id == employeeId && e.Account.Role.Name == "Employee");
        }

        private async Task SaveContractFileAsync(TeacherContract contract, IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return;

            DeleteStoredFile(contract.StoredFileName);
            var folder = GetContractFolder();
            Directory.CreateDirectory(folder);
            var storedName = $"contract_{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
            var path = Path.Combine(folder, storedName);

            await using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

            contract.OriginalFileName = Path.GetFileName(file.FileName);
            contract.StoredFileName = storedName;
            contract.ContentType = file.ContentType;
            contract.FileSize = file.Length;
            contract.UploadedAtUtc = DateTime.UtcNow;
        }

        private void DeleteStoredFile(string? storedFileName)
        {
            if (string.IsNullOrWhiteSpace(storedFileName))
                return;

            var path = Path.Combine(GetContractFolder(), storedFileName);
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }

        private static string GetContractFolder()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "contracts");
        }

        private static object ToDto(TeacherContract c)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return new
            {
                id = c.Id,
                employeeId = c.EmployeeId,
                contractNumber = c.ContractNumber,
                contractType = c.ContractType,
                signedDate = c.SignedDate.ToString("yyyy-MM-dd"),
                effectiveDate = c.EffectiveDate.ToString("yyyy-MM-dd"),
                expiryDate = c.ExpiryDate.HasValue ? c.ExpiryDate.Value.ToString("yyyy-MM-dd") : null,
                agreedSalary = c.AgreedSalary,
                workPosition = c.WorkPosition,
                workLocation = c.WorkLocation,
                workingHours = c.WorkingHours,
                status = c.Status.ToString(),
                terminationDate = c.TerminationDate.HasValue ? c.TerminationDate.Value.ToString("yyyy-MM-dd") : null,
                terminationReason = c.TerminationReason,
                note = c.Note,
                originalFileName = c.OriginalFileName,
                fileSize = c.FileSize,
                hasFile = c.StoredFileName != null,
                uploadedAtUtc = c.UploadedAtUtc,
                isExpiringSoon = c.Status == TeacherContractStatus.Active
                    && c.ExpiryDate.HasValue
                    && c.ExpiryDate.Value >= today
                    && c.ExpiryDate.Value <= today.AddDays(30),
                createdAtUtc = c.CreatedAtUtc,
                updatedAtUtc = c.UpdatedAtUtc
            };
        }
    }
}
