using datn.Models;
using System.ComponentModel.DataAnnotations;

namespace datn.Controllers.Manager
{
    public class CreateAssignmentViewModel
    {
        public int EmployeeId { get; set; }
        public int ClassId { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string? EndDate { get; set; }
        public string? RoleInClass { get; set; }
        public int? OldEmployeeId { get; set; }
        public int? OldClassId { get; set; }
        public string? OldStartDate { get; set; }
    }

    public class CreateStudentViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string DateOfBirth { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int ClassId { get; set; }
        public string EnrollDate { get; set; } = string.Empty;
        public IFormFile? Avatar { get; set; }
    }

    public class UpdateStudentViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string DateOfBirth { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int? ClassId { get; set; }
        public string EnrollDate { get; set; } = string.Empty;
        public int Status { get; set; } = 0;
        public IFormFile? Avatar { get; set; }
    }

    public class CreateTeacherViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        public string Username { get; set; } = string.Empty;
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [MinLength(9, ErrorMessage = "Mật khẩu phải có ít nhất 9 ký tự")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[!@#$%^&*()_+=\-\[\]{}|;:'"",.<>?/\\\\]).+$",
            ErrorMessage = "Mật khẩu phải chứa ít nhất 1 chữ hoa và 1 ký tự đặc biệt")]
        public string Password { get; set; } = string.Empty;
        [Required(ErrorMessage = "Họ đệm không được để trống")]
        public string FirstName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Tên không được để trống")]
        public string LastName { get; set; } = string.Empty;
        public bool Gender { get; set; }
        public string? DateOfBirth { get; set; }
        public string? Phone { get; set; }
        public TeacherType TeacherType { get; set; }
        public decimal? BaseSalary { get; set; }
        public IFormFile? Avatar { get; set; }
        public string? Bio { get; set; }
        public string? Qualifications { get; set; }
        public string? Experience { get; set; }
        public string? Philosophy { get; set; }
        public string? Specialty { get; set; }
        public bool ShowOnLanding { get; set; }
    }

    public class UpdateTeacherViewModel
    {
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Họ đệm không được để trống")]
        public string FirstName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Tên không được để trống")]
        public string LastName { get; set; } = string.Empty;
        public bool Gender { get; set; }
        public string? DateOfBirth { get; set; }
        public string? Phone { get; set; }
        public TeacherType TeacherType { get; set; }
        public decimal? BaseSalary { get; set; }
        public IFormFile? Avatar { get; set; }
        public string? Bio { get; set; }
        public string? Qualifications { get; set; }
        public string? Experience { get; set; }
        public string? Philosophy { get; set; }
        public string? Specialty { get; set; }
        public bool ShowOnLanding { get; set; }
    }

    public class SaveTeacherContractViewModel
    {
        public string ContractNumber { get; set; } = string.Empty;
        public string ContractType { get; set; } = string.Empty;
        public string SignedDate { get; set; } = string.Empty;
        public string EffectiveDate { get; set; } = string.Empty;
        public string? ExpiryDate { get; set; }
        public decimal? AgreedSalary { get; set; }
        public string? WorkPosition { get; set; }
        public string? WorkLocation { get; set; }
        public string? WorkingHours { get; set; }
        public TeacherContractStatus Status { get; set; } = TeacherContractStatus.Draft;
        public string? Note { get; set; }
        public IFormFile? File { get; set; }
    }

    public class TerminateTeacherContractViewModel
    {
        public string TerminationDate { get; set; } = string.Empty;
        public string TerminationReason { get; set; } = string.Empty;
    }

    public class SaveClassViewModel
    {
        public string Name { get; set; } = string.Empty;
        public int? AgeFrom { get; set; }
        public int? AgeTo { get; set; }
        public string? SchoolYear { get; set; }
        public int MaxCapacity { get; set; } = 25;
    }

    public class SaveSubjectViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class SaveClassScheduleViewModel
    {
        public int ClassId { get; set; }
        public int SubjectId { get; set; }
        public int? EmployeeId { get; set; }
        public int DayOfWeek { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public int? LocationId { get; set; }
        public string EffectiveFrom { get; set; } = string.Empty;
        public string? EffectiveTo { get; set; }
        public string? Note { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class SaveActivityViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Date { get; set; } = string.Empty;
        public int? LocationId { get; set; }
        public int? OrganizerId { get; set; }
        public List<int>? ClassIds { get; set; }
    }
}
