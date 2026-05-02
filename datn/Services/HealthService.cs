using datn.Data;
using datn.Models;
using Microsoft.EntityFrameworkCore;

namespace datn.Services
{
    public class HealthService : IHealthService
    {
        private readonly AppDbContext _context;
        public HealthService(AppDbContext context) => _context = context;

        public async Task<List<HealthRecord>> GetHistoryAsync(int studentId)
        {
            return await _context.HealthRecords
                .Where(hr => hr.StudentId == studentId)
                .OrderByDescending(hr => hr.Date)
                .ToListAsync();
        }

        public async Task<bool> SaveRecordAsync(HealthRecord record)
        {
            var existing = await _context.HealthRecords.FindAsync(record.StudentId, record.Date);
            if (existing == null)
            {
                _context.HealthRecords.Add(record);
            }
            else
            {
                _context.Entry(existing).CurrentValues.SetValues(record);
            }
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<HealthRecord?> GetLatestRecordAsync(int studentId)
        {
            return await _context.HealthRecords
                .Where(hr => hr.StudentId == studentId)
                .OrderByDescending(hr => hr.Date)
                .FirstOrDefaultAsync();
        }
    }
}
