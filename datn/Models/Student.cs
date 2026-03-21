using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace datn.Models
{
    public class Student
    {
        public int Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Address { get; set; }
        public int? ClassId { get; set; }
        public DateOnly? EnrollDate { get; set; }

        public Class? Class { get; set; }
        public ICollection<ParentStudent> ParentStudents { get; set; }
        public ICollection<Tuition> Tuitions { get; set; }
        public ICollection<Attendance> Attendances { get; set; }
        public ICollection<StudyReport> StudyReports { get; set; }
        public ICollection<HealthRecord> HealthRecords { get; set; }
    }
}
