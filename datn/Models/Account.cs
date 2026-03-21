namespace datn.Models
{
    public class Account
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; } = true;
        public int RoleId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Role Role { get; set; }
        public ICollection<RefreshToken> RefreshTokens { get; set; }
        public Employee? Employee { get; set; }
        public Parent? Parent { get; set; }
    }
}
