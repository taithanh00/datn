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
        public string? AvatarPath { get; set; }

        public Account Account { get; set; }
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        public ICollection<WorkAttendance> WorkAttendances { get; set; } = new List<WorkAttendance>();
        public ICollection<Salary> Salaries { get; set; } = new List<Salary>();
        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public ICollection<StudyReport> StudyReports { get; set; } = new List<StudyReport>();
        public ICollection<Activity> Activities { get; set; } = new List<Activity>();
        public ICollection<EmployeeLeaveRequest> LeaveRequests { get; set; } = new List<EmployeeLeaveRequest>();
        public ICollection<ClassSchedule> ClassSchedules { get; set; } = new List<ClassSchedule>();
    }
}
