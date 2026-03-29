using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using datn.Models;
using Microsoft.IdentityModel.Tokens;
namespace datn.Services
{
    /// <summary>
    /// JwtService: Tạo và quản lý JWT tokens
    /// - GenerateAccessToken: Tạo JWT Access Token chứa thông tin user
    /// - GenerateRefreshToken: Tạo Refresh Token dùng để lấy Access Token mới
    /// </summary>
    public class JwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// GenerateAccessToken: Tạo JWT Access Token
        /// JWT Access Token chứa:
        /// - Header: kiểu token (JWT), thuật toán ký (HS256)
        /// - Payload: 
        ///   + NameIdentifier: Id của account
        ///   + Name: Username
        ///   + Email: Email
        ///   + Role: Vai trò (Manager, Employee, Parent, ...)
        /// - Signature: Được ký bằng SecretKey để đảm bảo tính toàn vẹn
        /// 
        /// Thời hạn: Ngắn (vài phút) để bảo mật cao
        /// Khi hết hạn: Dùng Refresh Token để lấy token mới
        /// </summary>
        public string GenerateAccessToken(Account account)
        {
            var jwtSettings = _config.GetSection("JwtSettings");

            // Tạo signing key từ SecretKey (phải dài và bí mật)
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]));

            // Tạo danh sách Claims (các thông tin cần mã hóa vào token)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),  // Id của account
                new Claim(ClaimTypes.Name,           account.Username),        // Tên đăng nhập
                new Claim(ClaimTypes.Email,          account.Email),           // Email
                new Claim(ClaimTypes.Role,           account.Role.Name)        // Vai trò
            };

            // Tạo JWT token
            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],           // Ai phát hành token
                audience: jwtSettings["Audience"],       // Ai dùng token
                claims: claims,                          // Thông tin cần bảo vệ
                expires: DateTime.UtcNow.AddMinutes(
                                        int.Parse(jwtSettings["AccessTokenExpiryMinutes"])), // Thời hạn
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256) // Ký token
            );

            // Chuyển token thành chuỗi để gửi cho client
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// GenerateRefreshToken: Tạo Refresh Token ngẫu nhiên
        /// Refresh Token:
        /// - Là một chuỗi random dài 64 bytes (không phải JWT)
        /// - Được lưu vào DB
        /// - Được gửi tới client qua cookie
        /// - Khi Access Token hết hạn, dùng Refresh Token để lấy Access Token mới
        /// - Có thể thu hồi (revoke) nếu nghi ngờ bị đánh cắp
        /// </summary>
        public string GenerateRefreshToken()
        {
            // Tạo 64 bytes ngẫu nhiên (an toàn mật mã)
            var bytes = RandomNumberGenerator.GetBytes(64);

            // Chuyển sang Base64 string để có thể lưu và truyền dễ dàng
            return Convert.ToBase64String(bytes);
        }
    }
}
