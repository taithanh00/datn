using datn.DTOs;
using datn.Models;

namespace datn.Services
{
    public interface IParentService
    {
        Task<Parent> CreateParentAsync(CreateParentDto dto);
        Task<Parent?> UpdateParentAsync(int id, CreateParentDto dto);
        Task<bool> DeleteParentAsync(int id);
        Task<bool> LinkStudentAsync(int parentId, int studentId, string relationship);
        Task<bool> UnlinkStudentAsync(int parentId, int studentId);
        Task<bool> IsEmailOrUsernameExists(string email, string username, int? excludeParentId = null);
    }
}
