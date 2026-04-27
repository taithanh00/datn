using datn.Data;
using datn.DTOs;
using datn.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace datn.Services
{
    public class ParentService : IParentService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ParentService(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<bool> IsEmailOrUsernameExists(string email, string username, int? excludeParentId = null)
        {
            var query = _context.Accounts.AsQueryable();
            if (excludeParentId.HasValue)
            {
                var parent = await _context.Parents.FindAsync(excludeParentId.Value);
                if (parent != null)
                {
                    query = query.Where(a => a.Id != parent.AccountId);
                }
            }

            return await query.AnyAsync(a => a.Email == email || a.Username == username);
        }

        public async Task<Parent> CreateParentAsync(CreateParentDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Create Account
                var parentRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Parent");
                if (parentRole == null) throw new Exception("Vai trò 'Parent' không tồn tại trong hệ thống.");

                var account = new Account
                {
                    Username = dto.Username,
                    Email = dto.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                    PasswordSalt = "",
                    RoleId = parentRole.Id,
                    IsActive = true,
                    MustChangePassword = true,
                    CreatedAt = DateTime.Now
                };
                _context.Accounts.Add(account);
                await _context.SaveChangesAsync();

                // 2. Create Parent
                var parent = new Parent
                {
                    AccountId = account.Id,
                    FirstName = dto.FirstName.Trim(),
                    LastName = dto.LastName.Trim(),
                    Phone = dto.Phone ?? "",
                    Address = dto.Address ?? "",
                    ParentStudents = new List<ParentStudent>()
                };

                if (dto.Avatar != null)
                {
                    parent.AvatarPath = await SaveAvatarAsync(dto.Avatar, "parent");
                }

                _context.Parents.Add(parent);
                await _context.SaveChangesAsync();

                // 3. Link students if provided
                if (dto.StudentLinks != null && dto.StudentLinks.Any())
                {
                    foreach (var link in dto.StudentLinks)
                    {
                        var student = await _context.Students.FindAsync(link.StudentId);
                        if (student != null)
                        {
                            _context.ParentStudents.Add(new ParentStudent
                            {
                                Parent = parent,    
                                Student = student,
                                Relationship = link.Relationship
                            });
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return parent;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Parent?> UpdateParentAsync(int id, CreateParentDto dto)
        {
            var parent = await _context.Parents.Include(p => p.Account).FirstOrDefaultAsync(p => p.Id == id);
            if (parent == null) return null;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Update Account info
                parent.Account.Email = dto.Email;
                parent.Account.Username = dto.Username;
                if (!string.IsNullOrEmpty(dto.Password) && dto.Password != "******")
                {
                    parent.Account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password.Trim());
                }

                // Update Parent info
                parent.FirstName = dto.FirstName.Trim();
                parent.LastName = dto.LastName.Trim();
                parent.Phone = dto.Phone ?? "";
                parent.Address = dto.Address ?? "";

                if (dto.Avatar != null)
                {
                    parent.AvatarPath = await SaveAvatarAsync(dto.Avatar, "parent");
                }

                _context.Parents.Update(parent);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return parent;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteParentAsync(int id)
        {
            var parent = await _context.Parents
                .Include(p => p.Account)
                .FirstOrDefaultAsync(p => p.Id == id);
                
            if (parent == null) return false;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                parent.Account.IsActive = false;
                var activeTokens = await _context.RefreshTokens
                    .Where(r => r.AccountId == parent.AccountId && !r.IsRevoked)
                    .ToListAsync();
                foreach (var token in activeTokens)
                {
                    token.IsRevoked = true;
                }
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<bool> LinkStudentAsync(int parentId, int studentId, string relationship)
        {
            // Kiểm tra parent có active không
            var parent = await _context.Parents
                .Include(p => p.Account)
                .FirstOrDefaultAsync(p => p.Id == parentId);

            if (parent == null || !parent.Account.IsActive)
                throw new InvalidOperationException("Tài khoản phụ huynh đã bị vô hiệu hóa.");

            // Kiểm tra student có active không
            var student = await _context.Students.FindAsync(studentId);
            if (student == null || student.Status == StudentStatus.Inactive)
                throw new InvalidOperationException("Học sinh này đã nghỉ học.");

            // Kiểm tra đã có bố/mẹ chưa
            if (relationship == "Bố" || relationship == "Mẹ")
            {
                var roleExists = await _context.ParentStudents
                    .AnyAsync(ps => ps.StudentId == studentId && ps.Relationship == relationship);

                if (roleExists)
                    throw new InvalidOperationException($"Học sinh này đã có {relationship} được liên kết.");
            }

            // Kiểm tra liên kết này đã tồn tại chưa
            var alreadyLinked = await _context.ParentStudents
                .AnyAsync(ps => ps.ParentId == parentId && ps.StudentId == studentId);
            if (alreadyLinked) return true;

            _context.ParentStudents.Add(new ParentStudent
            {
                ParentId = parentId,
                StudentId = studentId,
                Relationship = relationship,
                Parent = parent,
                Student = student
            });

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UnlinkStudentAsync(int parentId, int studentId)
        {
            var link = await _context.ParentStudents.FirstOrDefaultAsync(ps => ps.ParentId == parentId && ps.StudentId == studentId);
            if (link == null) return true;

            _context.ParentStudents.Remove(link);
            return await _context.SaveChangesAsync() > 0;
        }

        private async Task<string> SaveAvatarAsync(IFormFile file, string prefix)
        {
            var folderPath = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
            Directory.CreateDirectory(folderPath);

            var fileName = $"{prefix}_{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
            var path = Path.Combine(folderPath, fileName);
            await using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/uploads/avatars/{fileName}";
        }
    }
}
