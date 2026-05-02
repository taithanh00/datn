using datn.Models;

namespace datn.Services
{
    public interface IHealthService
    {
        Task<List<HealthRecord>> GetHistoryAsync(int studentId);
        Task<bool> SaveRecordAsync(HealthRecord record);
        Task<HealthRecord?> GetLatestRecordAsync(int studentId);
    }

    public interface IDailyReportService
    {
        Task<DailyReport?> GetReportAsync(int studentId, DateOnly date);
        Task<bool> SaveReportAsync(DailyReport report);
        Task<List<DailyReport>> GetClassReportsAsync(int classId, DateOnly date);
        Task<bool> BatchCreateReportsAsync(int classId, DateOnly date, DailyReport template);
    }
}
