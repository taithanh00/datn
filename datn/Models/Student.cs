using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace datn.Models
{
    public class Student
    {
        public int Id { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool Gender { get; set; }
        public DateOnly DateOfBirth { get; set; }
        public string? Address { get; set; }
        public string? FatherName { get; set; }
        public string? MotherName { get; set; }
        public int? ClassId { get; set; }
        public DateOnly? EnrollDate { get; set; }
        public string? AvatarPath { get; set; }
        public StudentStatus Status { get; set; } = StudentStatus.Active;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Class? Class { get; set; }
        public ICollection<ParentStudent> ParentStudents { get; set; }
        public ICollection<Tuition> Tuitions { get; set; }
        public ICollection<Attendance> Attendances { get; set; }
        public ICollection<StudyReport> StudyReports { get; set; }
        public ICollection<HealthRecord> HealthRecords { get; set; }
        public ICollection<StudentActivity> StudentActivities { get; set; } = new List<StudentActivity>();
    }

    public enum StudentStatus
    {
        Active = 0,      // Đang học
        Inactive = 1     // Đã nghỉ
    }
}
