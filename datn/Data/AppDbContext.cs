using datn.Models;
using Microsoft.EntityFrameworkCore;

namespace datn.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Role> Roles { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Parent> Parents { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<ParentStudent> ParentStudents { get; set; }
        public DbSet<TuitionPlan> TuitionPlans { get; set; }
        public DbSet<Tuition> Tuitions { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Ranking> Rankings { get; set; }
        public DbSet<StudyReport> StudyReports { get; set; }
        public DbSet<HealthRecord> HealthRecords { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<WorkAttendance> WorkAttendances { get; set; }
        public DbSet<PayrollPeriod> PayrollPeriods { get; set; }
        public DbSet<Salary> Salaries { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Activity> Activities { get; set; }
        public DbSet<ClassActivity> ClassActivities { get; set; }
        public DbSet<Curriculum> Curriculums { get; set; }
        public DbSet<TeachingPlan> TeachingPlans { get; set; }
        public DbSet<EmployeeLeaveRequest> EmployeeLeaveRequests { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<ClassSchedule> ClassSchedules { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<StudentActivity> StudentActivities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Notification ──────────────────────────────────────
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Recipient)
                .WithMany()
                .HasForeignKey(n => n.RecipientId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Account ──────────────────────────────────────────
            modelBuilder.Entity<Account>()
                .HasOne(a => a.Role)
                .WithMany(r => r.Accounts)
                .HasForeignKey(a => a.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Account>()
                .HasIndex(a => a.Username)
                .IsUnique();

            modelBuilder.Entity<Account>()
                .HasIndex(a => a.Email)
                .IsUnique();

            // ── RefreshToken ──────────────────────────────────────
            modelBuilder.Entity<RefreshToken>()
                .HasOne(r => r.Account)
                .WithMany(a => a.RefreshTokens)
                .HasForeignKey(r => r.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Employee ──────────────────────────────────────────
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Account)
                .WithOne(a => a.Employee)
                .HasForeignKey<Employee>(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Parent ────────────────────────────────────────────
            modelBuilder.Entity<Parent>()
                .HasOne(p => p.Account)
                .WithOne(a => a.Parent)
                .HasForeignKey<Parent>(p => p.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Student ───────────────────────────────────────────
            modelBuilder.Entity<Student>()
                .HasOne(s => s.Class)
                .WithMany(c => c.Students)
                .HasForeignKey(s => s.ClassId)
                .OnDelete(DeleteBehavior.SetNull);

            // ── ParentStudent (composite PK) ──────────────────────
            modelBuilder.Entity<ParentStudent>()
                .HasKey(ps => new { ps.ParentId, ps.StudentId });

            modelBuilder.Entity<ParentStudent>()
                .HasOne(ps => ps.Parent)
                .WithMany(p => p.ParentStudents)
                .HasForeignKey(ps => ps.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ParentStudent>()
                .HasOne(ps => ps.Student)
                .WithMany(s => s.ParentStudents)
                .HasForeignKey(ps => ps.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Tuition (unique: StudentId + Month + Year) ─────────
            modelBuilder.Entity<Tuition>()
                .HasOne(t => t.Student)
                .WithMany(s => s.Tuitions)
                .HasForeignKey(t => t.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Tuition>()
                .HasOne(t => t.TuitionPlan)
                .WithMany(tp => tp.Tuitions)
                .HasForeignKey(t => t.TuitionPlanId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Tuition>()
                .HasIndex(t => new { t.StudentId, t.Month, t.Year })
                .IsUnique();

            // ── Attendance (composite PK) ─────────────────────────
            modelBuilder.Entity<Attendance>()
                .HasKey(a => new { a.StudentId, a.Date });

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Student)
                .WithMany(s => s.Attendances)
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Employee)
                .WithMany(e => e.Attendances)
                .HasForeignKey(a => a.TakenBy)
                .OnDelete(DeleteBehavior.SetNull);

            // ── StudyReport (composite PK) ────────────────────────
            modelBuilder.Entity<StudyReport>()
                .HasKey(sr => new { sr.StudentId, sr.Date });

            modelBuilder.Entity<StudyReport>()
                .HasOne(sr => sr.Student)
                .WithMany(s => s.StudyReports)
                .HasForeignKey(sr => sr.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudyReport>()
                .HasOne(sr => sr.Ranking)
                .WithMany(r => r.StudyReports)
                .HasForeignKey(sr => sr.RankingId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<StudyReport>()
                .HasOne(sr => sr.Teacher)
                .WithMany(e => e.StudyReports)
                .HasForeignKey(sr => sr.TeacherId)
                .OnDelete(DeleteBehavior.SetNull);

            // ── HealthRecord (composite PK) ───────────────────────
            modelBuilder.Entity<HealthRecord>()
                .HasKey(hr => new { hr.StudentId, hr.Date });

            modelBuilder.Entity<HealthRecord>()
                .HasOne(hr => hr.Student)
                .WithMany(s => s.HealthRecords)
                .HasForeignKey(hr => hr.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Assignment (composite PK) ─────────────────────────
            modelBuilder.Entity<Assignment>()
                .HasKey(a => new { a.EmployeeId, a.ClassId, a.StartDate });

            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.Employee)
                .WithMany(e => e.Assignments)
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.Class)
                .WithMany(c => c.Assignments)
                .HasForeignKey(a => a.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── WorkAttendance (composite PK) ─────────────────────
            modelBuilder.Entity<WorkAttendance>()
                .HasKey(wa => new { wa.EmployeeId, wa.Date });

            modelBuilder.Entity<WorkAttendance>()
                .HasOne(wa => wa.Employee)
                .WithMany(e => e.WorkAttendances)
                .HasForeignKey(wa => wa.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── EmployeeLeaveRequest ───────────────────────────────
            modelBuilder.Entity<EmployeeLeaveRequest>()
                .HasOne(lr => lr.Employee)
                .WithMany(e => e.LeaveRequests)
                .HasForeignKey(lr => lr.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Salary (composite PK) ─────────────────────────────
            modelBuilder.Entity<Salary>()
                .HasKey(s => new { s.EmployeeId, s.PayrollPeriodId });

            modelBuilder.Entity<Salary>()
                .HasOne(s => s.Employee)
                .WithMany(e => e.Salaries)
                .HasForeignKey(s => s.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Salary>()
                .HasOne(s => s.PayrollPeriod)
                .WithMany(pp => pp.Salaries)
                .HasForeignKey(s => s.PayrollPeriodId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Activity ──────────────────────────────────────────
            modelBuilder.Entity<Activity>()
                .HasOne(a => a.Location)
                .WithMany(l => l.Activities)
                .HasForeignKey(a => a.LocationId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Activity>()
                .HasOne(a => a.Organizer)
                .WithMany(e => e.Activities)
                .HasForeignKey(a => a.OrganizerId)
                .OnDelete(DeleteBehavior.SetNull);

            // ── ClassActivity (composite PK) ──────────────────────
            modelBuilder.Entity<ClassActivity>()
                .HasKey(ca => new { ca.ClassId, ca.ActivityId });

            modelBuilder.Entity<ClassActivity>()
                .HasOne(ca => ca.Class)
                .WithMany(c => c.ClassActivities)
                .HasForeignKey(ca => ca.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClassActivity>()
                .HasOne(ca => ca.Activity)
                .WithMany(a => a.ClassActivities)
                .HasForeignKey(ca => ca.ActivityId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Curriculum ────────────────────────────────────────
            modelBuilder.Entity<Curriculum>()
                .HasOne(c => c.Subject)
                .WithMany()
                .HasForeignKey(c => c.SubjectId)
                .OnDelete(DeleteBehavior.SetNull);

            // ── TeachingPlan (composite PK) ───────────────────────
            modelBuilder.Entity<TeachingPlan>()
                .HasKey(tp => new { tp.ClassId, tp.CurriculumId, tp.StartDate });

            modelBuilder.Entity<TeachingPlan>()
                .HasOne(tp => tp.Class)
                .WithMany(c => c.TeachingPlans)
                .HasForeignKey(tp => tp.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TeachingPlan>()
                .HasOne(tp => tp.Curriculum)
                .WithMany(c => c.TeachingPlans)
                .HasForeignKey(tp => tp.CurriculumId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Subject ───────────────────────────────────────────
            modelBuilder.Entity<Subject>()
                .HasIndex(s => s.Code)
                .IsUnique();

            // ── ClassSchedule ─────────────────────────────────────
            modelBuilder.Entity<ClassSchedule>()
                .HasOne(cs => cs.Class)
                .WithMany(c => c.ClassSchedules)
                .HasForeignKey(cs => cs.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClassSchedule>()
                .HasOne(cs => cs.Subject)
                .WithMany(s => s.ClassSchedules)
                .HasForeignKey(cs => cs.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClassSchedule>()
                .HasOne(cs => cs.Employee)
                .WithMany(e => e.ClassSchedules)
                .HasForeignKey(cs => cs.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClassSchedule>()
                .HasIndex(cs => new
                {
                    cs.ClassId,
                    cs.DayOfWeek,
                    cs.StartTime,
                    cs.EndTime,
                    cs.EffectiveFrom
                });

            // ── StudentActivity (composite PK) ───────────────────
            modelBuilder.Entity<StudentActivity>()
                .HasKey(sa => new { sa.StudentId, sa.ActivityId });

            modelBuilder.Entity<StudentActivity>()
                .HasOne(sa => sa.Student)
                .WithMany(s => s.StudentActivities)
                .HasForeignKey(sa => sa.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentActivity>()
                .HasOne(sa => sa.Activity)
                .WithMany(a => a.StudentActivities)
                .HasForeignKey(sa => sa.ActivityId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Seed Roles ────────────────────────────────────────
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Manager", Description = "Quản lý" },
                new Role { Id = 2, Name = "Employee", Description = "Giáo viên" },
                new Role { Id = 3, Name = "Parent", Description = "Phụ huynh" }
            );
        }
    }
}
