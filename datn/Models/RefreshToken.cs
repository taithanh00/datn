namespace datn.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Account Account { get; set; }
    }
}
