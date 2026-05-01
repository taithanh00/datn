using datn.Models;

namespace datn.Services
{
    public interface IEducationService
    {
        /// <summary>
        /// Lấy bài học hiện tại của một lớp dựa trên môn học và ngày cụ thể.
        /// </summary>
        Task<Curriculum?> GetCurrentLessonAsync(int classId, int subjectId, DateOnly date);
    }
}
