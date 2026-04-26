using datn.Data;
using datn.DTOs;
using datn.Models;
using Microsoft.EntityFrameworkCore;

namespace datn.Services
{
    public class StudentService : IStudentService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public StudentService(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<string> GenerateStudentCodeAsync(int year)
        {
            // Ignore global filter to include "deleted" students in the count for unique codes
            var count = await _context.Students
                .IgnoreQueryFilters()
                .CountAsync(s => s.EnrollDate.HasValue && s.EnrollDate.Value.Year == year);
            
            return $"{year}{(count + 1):D4}";
        }

        public async Task<Student?> CheckPotentialDuplicateAsync(CreateStudentDto dto)
        {
            if (!DateOnly.TryParse(dto.DateOfBirth, out var dob)) return null;

            var firstName = dto.FirstName.Trim().ToLower();
            var lastName = dto.LastName.Trim().ToLower();
            var motherName = dto.MotherName?.Trim().ToLower();
            var fatherName = dto.FatherName?.Trim().ToLower();
            var gender = dto.Gender == "true";

            return await _context.Students.FirstOrDefaultAsync(s =>
                s.FirstName.ToLower() == firstName &&
                s.LastName.ToLower() == lastName &&
                s.DateOfBirth == dob &&
                s.Gender == gender &&
                s.MotherName.ToLower() == motherName &&
                s.FatherName.ToLower() == fatherName);
        }

        public async Task<Student> CreateStudentAsync(CreateStudentDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var enrollDate = string.IsNullOrEmpty(dto.EnrollDate) 
                    ? DateOnly.FromDateTime(DateTime.Now) 
                    : DateOnly.Parse(dto.EnrollDate);
                
                var studentCode = await GenerateStudentCodeAsync(enrollDate.Year);

                var student = new Student
                {
                    StudentCode = studentCode,
                    FirstName = dto.FirstName.Trim(),
                    LastName = dto.LastName.Trim(),
                    Gender = dto.Gender == "true",
                    DateOfBirth = DateOnly.Parse(dto.DateOfBirth),
                    Address = dto.Address,
                    FatherName = dto.FatherName,
                    MotherName = dto.MotherName,
                    ClassId = dto.ClassId > 0 ? dto.ClassId : null,
                    EnrollDate = enrollDate,
                    Status = (StudentStatus)dto.Status,
                    CreatedAt = DateTime.Now
                };

                if (dto.Avatar != null)
                {
                    student.AvatarPath = await SaveAvatarAsync(dto.Avatar, "student");
                }

                _context.Students.Add(student);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return student;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Student?> GetStudentByIdAsync(int id)
        {
            return await _context.Students.Include(s => s.Class).FirstOrDefaultAsync(s => s.Id == id);
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
