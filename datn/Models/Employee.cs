using System.Diagnostics;

namespace datn.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string FullName { get; set; }
        public string? Phone { get; set; }
        public string? Position { get; set; }
        public decimal? BaseSalary { get; set; }

        public Account Account { get; set; }
        public ICollection<Assignment> Assignments { get; set; }
        public ICollection<WorkAttendance> WorkAttendances { get; set; }
        public ICollection<Salary> Salaries { get; set; }
        public ICollection<Attendance> Attendances { get; set; }
        public ICollection<StudyReport> StudyReports { get; set; }
        public ICollection<Activity> Activities { get; set; }
    }
}
