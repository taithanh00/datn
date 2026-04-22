namespace datn.Models
{
    /// <summary>
    /// RefreshToken: Mô hình dữ liệu cho Refresh Token
    /// 
    /// Mục đích:
    /// - Lưu trữ Refresh Token trong cơ sở dữ liệu
    /// - Cho phép thu hồi (revoke) token khi cần thiết
    /// - Theo dõi thời hạn và trạng thái của token
    /// 
    /// Luồng hoạt động:
    /// 1. User đăng nhập → Tạo Refresh Token mới → Lưu vào DB
    /// 2. Access Token hết hạn → Dùng Refresh Token từ cookie
    /// 3. Kiểm tra Refresh Token trong DB (chưa thu hồi, chưa hết hạn) → Cấp Access Token mới
    /// 4. User đăng xuất → Đánh dấu Refresh Token là IsRevoked = true
    /// </summary>
    public class RefreshToken
    {
        /// <summary>
        /// Id: Khóa chính, tự động tăng
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// AccountId: Khóa ngoại liên kết đến Account
        /// Xác định Refresh Token này thuộc về tài khoản nào
        /// </summary>
        public int AccountId { get; set; }

        /// <summary>
        /// Token: Chuỗi Refresh Token (random 64 bytes, Base64 encoded)
        /// Là giá trị được gửi tới client và lưu trong cookie
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// ExpiresAt: Thời điểm hết hạn của Refresh Token
        /// Khi DateTime.UtcNow > ExpiresAt thì token không còn hợp lệ
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// IsRevoked: Cờ đánh dấu token đã bị thu hồi hay chưa
        /// - false: Token còn hợp lệ
        /// - true: Token đã bị thu hồi (đăng xuất, hoặc nghi ngờ bị đánh cắp)
        /// Mặc định: false
        /// </summary>
        public bool IsRevoked { get; set; } = false;
        public DateTime? RevokedAtUtc { get; set; }

        /// <summary>
        /// CreatedAt: Thời điểm tạo Refresh Token
        /// Dùng để tính thời gian sống của token
        /// Mặc định: Thời gian hiện tại (UTC)
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Account: Tham chiếu đến entity Account (quan hệ 1-nhiều)
        /// Dùng để truy cập thông tin tài khoản khi cần
        /// </summary>
        public Account Account { get; set; }
    }
}
