using datn.Data;
using datn.Models;
using Microsoft.EntityFrameworkCore;

namespace datn.Services
{
    public class DailyReportService : IDailyReportService
    {
        private readonly AppDbContext _context;
        public DailyReportService(AppDbContext context) => _context = context;

        public async Task<DailyReport?> GetReportAsync(int studentId, DateOnly date)
        {
            return await _context.DailyReports
                .FirstOrDefaultAsync(dr => dr.StudentId == studentId && dr.Date == date);
        }

        public async Task<bool> SaveReportAsync(DailyReport report)
        {
            if (report.Id == 0) _context.DailyReports.Add(report);
            else _context.DailyReports.Update(report);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<List<DailyReport>> GetClassReportsAsync(int classId, DateOnly date)
        {
            return await _context.DailyReports
                .Include(dr => dr.Student)
                .Where(dr => dr.Student.ClassId == classId && dr.Date == date)
                .ToListAsync();
        }

        public async Task<bool> BatchCreateReportsAsync(int classId, DateOnly date, DailyReport template)
        {
            var students = await _context.Students
                .Where(s => s.ClassId == classId && s.Status == StudentStatus.Active)
                .ToListAsync();

            foreach (var student in students)
            {
                var existing = await _context.DailyReports.FirstOrDefaultAsync(dr => dr.StudentId == student.Id && dr.Date == date);
                if (existing == null)
                {
                    _context.DailyReports.Add(new DailyReport
                    {
                        StudentId = student.Id,
                        Date = date,
                        EatingStatus = template.EatingStatus,
                        EatingNote = template.EatingNote,
                        SleepingStatus = template.SleepingStatus,
                        SleepingNote = template.SleepingNote,
                        HygieneNote = template.HygieneNote,
                        MoodNote = template.MoodNote
                    });
                }
            }
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
