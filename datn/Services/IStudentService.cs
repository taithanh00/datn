using datn.DTOs;
using datn.Models;

namespace datn.Services
{
    public interface IStudentService
    {
        Task<Student?> CheckPotentialDuplicateAsync(CreateStudentDto dto);
        Task<Student> CreateStudentAsync(CreateStudentDto dto);
        Task<Student?> GetStudentByIdAsync(int id);
    }
}
