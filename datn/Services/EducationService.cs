using datn.Data;
using datn.Models;
using Microsoft.EntityFrameworkCore;

namespace datn.Services
{
    public class EducationService : IEducationService
    {
        private readonly AppDbContext _context;

        public EducationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Curriculum?> GetCurrentLessonAsync(int classId, int subjectId, DateOnly date)
        {
            // Tìm trong kế hoạch giảng dạy của lớp
            // Khớp với môn học (thông qua Curriculum)
            // Và ngày hiện tại nằm trong khoảng StartDate - EndDate
            var teachingPlan = await _context.TeachingPlans
                .Include(tp => tp.Curriculum)
                .Where(tp => tp.ClassId == classId && 
                            tp.Curriculum.SubjectId == subjectId &&
                            tp.StartDate <= date && 
                            (tp.EndDate == null || tp.EndDate >= date))
                .OrderByDescending(tp => tp.StartDate) // Lấy kế hoạch mới nhất nếu có trùng lặp
                .FirstOrDefaultAsync();

            return teachingPlan?.Curriculum;
        }
    }
}
