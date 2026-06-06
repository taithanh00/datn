using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace datn.Models
{
    public enum TeacherType
    {
        Lead,    // Giáo viên Chủ nhiệm
        Subject  // Giáo viên Bộ môn
    }
    public class Employee
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        [Required]
        public string FirstName { get; set; } = string.Empty;
        [Required]
        public string LastName { get; set; } = string.Empty;

        [NotMapped]
        public string FullName => $"{LastName} {FirstName}".Trim();
        public string? Phone { get; set; }
        public decimal? BaseSalary { get; set; }
        public string? AvatarPath { get; set; }
        public bool IsActive { get; set; } = true;
        public bool Gender { get; set; } // true: Male, false: Female
        public DateOnly? DateOfBirth { get; set; }

        // Các trường phục vụ Landing Page
        public string? Bio { get; set; }                 // Giới thiệu ngắn
        public string? Qualifications { get; set; }      // Bằng cấp/Học vấn
        public string? Experience { get; set; }          // Kinh nghiệm
        public string? Philosophy { get; set; }          // Triết lý giáo dục
        public string? Specialty { get; set; }           // Chuyên môn chính
        public bool ShowOnLanding { get; set; } = false; // Hiển thị ra trang chủ

        // Phân loại giáo viên
        public TeacherType TeacherType { get; set; } = TeacherType.Subject;
        public string? SpecializedSubjects { get; set; } // JSON array of Subject IDs (dành cho GVBM)

        public Account Account { get; set; }
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        public ICollection<WorkAttendance> WorkAttendances { get; set; } = new List<WorkAttendance>();
        public ICollection<Salary> Salaries { get; set; } = new List<Salary>();
        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public ICollection<StudyReport> StudyReports { get; set; } = new List<StudyReport>();
        public ICollection<Activity> Activities { get; set; } = new List<Activity>();
        public ICollection<EmployeeLeaveRequest> LeaveRequests { get; set; } = new List<EmployeeLeaveRequest>();
        public ICollection<ClassSchedule> ClassSchedules { get; set; } = new List<ClassSchedule>();
        public ICollection<TeacherContract> TeacherContracts { get; set; } = new List<TeacherContract>();
    }
}
