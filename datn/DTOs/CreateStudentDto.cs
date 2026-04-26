using System.ComponentModel.DataAnnotations;

namespace datn.DTOs
{
    public class CreateStudentDto
    {
        [Required(ErrorMessage = "Tên học sinh là bắt buộc")]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ học sinh là bắt buộc")]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn giới tính")]
        public string Gender { get; set; } = string.Empty; // "true" or "false" from form

        [Required(ErrorMessage = "Ngày sinh là bắt buộc")]
        public string DateOfBirth { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? FatherName { get; set; }

        [StringLength(100)]
        public string? MotherName { get; set; }

        public int? ClassId { get; set; }

        public string? EnrollDate { get; set; }

        public IFormFile? Avatar { get; set; }

        public int Status { get; set; } = 0; // Default to Active

        // Flag to confirm creation even if duplicate is suspected
        public bool ForceCreate { get; set; } = false;
    }
}
