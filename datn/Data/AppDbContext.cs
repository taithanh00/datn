using datn.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Security.Claims;

namespace datn.Data
{
    public class AppDbContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor)
            : base(options) 
        { 
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<AuditLog> AuditLogs { get; set; }

        public DbSet<Role> Roles { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Parent> Parents { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<ParentStudent> ParentStudents { get; set; }
        public DbSet<Tuition> Tuitions { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Ranking> Rankings { get; set; }
        public DbSet<StudyReport> StudyReports { get; set; }
        public DbSet<HealthRecord> HealthRecords { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<WorkAttendance> WorkAttendances { get; set; }
        public DbSet<PayrollPeriod> PayrollPeriods { get; set; }
        public DbSet<Holiday> Holidays { get; set; }
        public DbSet<Salary> Salaries { get; set; }
        public DbSet<Substitution> Substitutions { get; set; }
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
        public DbSet<FeeItem> FeeItems { get; set; }
        public DbSet<StudentFeeConfig> StudentFeeConfigs { get; set; }
        public DbSet<TuitionDetail> TuitionDetails { get; set; }
        public DbSet<Menu> Menus { get; set; }
        public DbSet<MenuOverride> MenuOverrides { get; set; }
        public DbSet<DailyReport> DailyReports { get; set; }

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

            modelBuilder.Entity<Student>()
                .HasIndex(s => s.StudentCode)
                .IsUnique();

            // ── Class.LeadTeacher (GVCN) ─────────────────────────
            modelBuilder.Entity<Class>()
                .HasOne(c => c.LeadTeacher)
                .WithMany()
                .HasForeignKey(c => c.LeadTeacherId)
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
                .HasOne(cs => cs.Location)
                .WithMany()
                .HasForeignKey(cs => cs.LocationId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ClassSchedule>()
                .HasIndex(cs => new
                {
                    cs.ClassId,
                    cs.DayOfWeek,
                    cs.StartTime,
                    cs.EndTime,
                    cs.EffectiveFrom
                });

            // ── Substitution ──────────────────────────────────────
            modelBuilder.Entity<Substitution>()
                .HasOne(s => s.ClassSchedule)
                .WithMany()
                .HasForeignKey(s => s.ClassScheduleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Substitution>()
                .HasOne(s => s.OriginalEmployee)
                .WithMany()
                .HasForeignKey(s => s.OriginalEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Substitution>()
                .HasOne(s => s.SubstituteEmployee)
                .WithMany()
                .HasForeignKey(s => s.SubstituteEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

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

            // ── FeeItem ──────────────────────────────────────────
            modelBuilder.Entity<FeeItem>()
                .HasIndex(fi => fi.Name)
                .IsUnique();

            // ── StudentFeeConfig ─────────────────────────────────
            modelBuilder.Entity<StudentFeeConfig>()
                .HasOne(sfc => sfc.Student)
                .WithMany(s => s.StudentFeeConfigs)
                .HasForeignKey(sfc => sfc.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentFeeConfig>()
                .HasOne(sfc => sfc.FeeItem)
                .WithMany(fi => fi.StudentFeeConfigs)
                .HasForeignKey(sfc => sfc.FeeItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── TuitionDetail ────────────────────────────────────
            modelBuilder.Entity<TuitionDetail>()
                .HasOne(td => td.Tuition)
                .WithMany(t => t.TuitionDetails)
                .HasForeignKey(td => td.TuitionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TuitionDetail>()
                .HasOne(td => td.FeeItem)
                .WithMany(fi => fi.TuitionDetails)
                .HasForeignKey(td => td.FeeItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TuitionDetail>()
                .HasOne(td => td.Subject)
                .WithMany(s => s.TuitionDetails)
                .HasForeignKey(td => td.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Menu & MenuOverride ──────────────────────────────
            modelBuilder.Entity<Menu>()
                .HasIndex(m => new { m.Date, m.MealType });

            modelBuilder.Entity<MenuOverride>()
                .HasOne(mo => mo.Menu)
                .WithMany(m => m.MenuOverrides)
                .HasForeignKey(mo => mo.MenuId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MenuOverride>()
                .HasOne(mo => mo.Student)
                .WithMany(s => s.MenuOverrides)
                .HasForeignKey(mo => mo.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MenuOverride>()
                .HasOne(mo => mo.Class)
                .WithMany()
                .HasForeignKey(mo => mo.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── DailyReport ──────────────────────────────────────
            modelBuilder.Entity<DailyReport>()
                .HasOne(dr => dr.Student)
                .WithMany(s => s.DailyReports)
                .HasForeignKey(dr => dr.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DailyReport>()
                .HasIndex(dr => new { dr.StudentId, dr.Date });

            // ── Seed Roles ────────────────────────────────────────
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Manager", Description = "Quản lý" },
                new Role { Id = 2, Name = "Employee", Description = "Giáo viên" },
                new Role { Id = 3, Name = "Parent", Description = "Phụ huynh" }
            );

            // ── Seed Rankings ──────────────────────────────────────
            modelBuilder.Entity<Ranking>().HasData(
                new Ranking { Id = 1, Name = "Đạt" },
                new Ranking { Id = 2, Name = "Cần cố gắng hơn" }
            );

            // ── Global Query Filters ──────────────────────────────
            modelBuilder.Entity<Account>().HasQueryFilter(a => a.IsActive);
            modelBuilder.Entity<Employee>().HasQueryFilter(e => e.IsActive);
            modelBuilder.Entity<Parent>().HasQueryFilter(p => p.IsActive);
            modelBuilder.Entity<Student>().HasQueryFilter(s => s.Status == StudentStatus.Active);
            modelBuilder.Entity<Class>().HasQueryFilter(c => c.IsActive);
            modelBuilder.Entity<Subject>().HasQueryFilter(s => s.IsActive);
            modelBuilder.Entity<FeeItem>().HasQueryFilter(f => f.IsActive);
            modelBuilder.Entity<Holiday>().HasQueryFilter(h => h.IsActive);
            modelBuilder.Entity<Location>().HasQueryFilter(l => l.IsActive);
            modelBuilder.Entity<Activity>().HasQueryFilter(a => a.IsActive);
            modelBuilder.Entity<Curriculum>().HasQueryFilter(c => c.IsActive);
            modelBuilder.Entity<TeachingPlan>().HasQueryFilter(tp => tp.IsActive);
            modelBuilder.Entity<Menu>().HasQueryFilter(m => m.IsActive);
            modelBuilder.Entity<MenuOverride>().HasQueryFilter(mo => mo.IsActive);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // 1. Capture audit entries before saving
            var auditEntries = OnBeforeSaveChanges();

            // 2. Perform the actual save (this generates IDs for new records)
            var result = await base.SaveChangesAsync(cancellationToken);

            // 3. Update audit entries with new IDs and save them
            if (auditEntries != null && auditEntries.Count > 0)
            {
                await OnAfterSaveChanges(auditEntries, cancellationToken);
            }

            return result;
        }

        private List<AuditEntry> OnBeforeSaveChanges()
        {
            ChangeTracker.DetectChanges();
            var auditEntries = new List<AuditEntry>();

            var user = _httpContextAccessor.HttpContext?.User;
            var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = user?.FindFirst("FullName")?.Value ?? user?.Identity?.Name;
            var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;

                Console.WriteLine($"[Audit] Detected change on {entry.Entity.GetType().Name} - State: {entry.State}");

                var auditEntry = new AuditEntry(entry)
                {
                    EntityName = entry.Metadata.ClrType.Name,
                    UserId = userId,
                    UserName = userName ?? "System", // Fallback
                    IpAddress = ipAddress,
                    AuditType = entry.State.ToString()
                };
                auditEntries.Add(auditEntry);

                foreach (var property in entry.Properties)
                {
                    string propertyName = property.Metadata.Name;

                    if (property.IsTemporary)
                    {
                        auditEntry.TemporaryProperties.Add(property);
                        continue;
                    }

                    if (property.Metadata.IsPrimaryKey())
                    {
                        auditEntry.KeyValues[propertyName] = property.CurrentValue;
                        continue;
                    }

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            auditEntry.NewValues[propertyName] = property.CurrentValue;
                            break;

                        case EntityState.Deleted:
                            auditEntry.OldValues[propertyName] = property.OriginalValue;
                            break;

                        case EntityState.Modified:
                            // Even if EF says not modified, we can check ourselves if values differ
                            bool isModified = property.IsModified || !Equals(property.OriginalValue, property.CurrentValue);
                            if (isModified)
                            {
                                auditEntry.ChangedColumns.Add(propertyName);
                                auditEntry.OldValues[propertyName] = property.OriginalValue;
                                auditEntry.NewValues[propertyName] = property.CurrentValue;
                                Console.WriteLine($"[Audit] Property {propertyName} changed: {property.OriginalValue} -> {property.CurrentValue}");
                            }
                            break;
                    }
                }
            }

            foreach (var auditEntry in auditEntries.Where(_ => !_.HasTemporaryProperties))
            {
                AuditLogs.Add(auditEntry.ToAudit());
            }

            return auditEntries.Where(_ => _.HasTemporaryProperties).ToList();
        }

        private Task OnAfterSaveChanges(List<AuditEntry> auditEntries, CancellationToken cancellationToken)
        {
            if (auditEntries == null || auditEntries.Count == 0)
                return Task.CompletedTask;

            foreach (var auditEntry in auditEntries)
            {
                foreach (var prop in auditEntry.TemporaryProperties)
                {
                    if (prop.Metadata.IsPrimaryKey())
                    {
                        auditEntry.KeyValues[prop.Metadata.Name] = prop.CurrentValue;
                    }
                    else
                    {
                        auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
                    }
                }
                AuditLogs.Add(auditEntry.ToAudit());
            }

            return base.SaveChangesAsync(cancellationToken);
        }

        private class AuditEntry
        {
            public AuditEntry(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
            {
                Entry = entry;
            }

            public Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry Entry { get; }
            public string? UserId { get; set; }
            public string? UserName { get; set; }
            public string EntityName { get; set; }
            public string AuditType { get; set; }
            public string IpAddress { get; set; }
            public Dictionary<string, object> KeyValues { get; } = new Dictionary<string, object>();
            public Dictionary<string, object> OldValues { get; } = new Dictionary<string, object>();
            public Dictionary<string, object> NewValues { get; } = new Dictionary<string, object>();
            public List<Microsoft.EntityFrameworkCore.ChangeTracking.PropertyEntry> TemporaryProperties { get; } = new List<Microsoft.EntityFrameworkCore.ChangeTracking.PropertyEntry>();
            public List<string> ChangedColumns { get; } = new List<string>();

            public bool HasTemporaryProperties => TemporaryProperties.Any();

            public AuditLog ToAudit()
            {
                var audit = new AuditLog();
                audit.UserId = UserId;
                audit.UserName = UserName;
                audit.Action = AuditType;
                audit.EntityName = EntityName;
                audit.CreatedAtUtc = DateTime.UtcNow;
                audit.EntityId = JsonSerializer.Serialize(KeyValues);
                audit.OldValues = OldValues.Count == 0 ? null : JsonSerializer.Serialize(OldValues);
                audit.NewValues = NewValues.Count == 0 ? null : JsonSerializer.Serialize(NewValues);
                audit.IpAddress = IpAddress;
                return audit;
            }
        }
    }
}
