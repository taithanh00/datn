using System.ComponentModel.DataAnnotations;

namespace datn.DTOs
{
    public class CreateParentDto
    {
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string Email { get; set; } = string.Empty;

        public string? Password { get; set; } = "123456"; // Default password

        [Required(ErrorMessage = "Tên là bắt buộc")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ là bắt buộc")]
        public string LastName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không đúng định dạng")]
        public string? Phone { get; set; }

        public string? Address { get; set; }
        
        public IFormFile? Avatar { get; set; }

        // Optional list of students to link immediately
        public List<StudentLinkDto>? StudentLinks { get; set; }
    }

    public class StudentLinkDto
    {
        public int StudentId { get; set; }
        public string Relationship { get; set; } = "Phụ huynh";
    }
}
