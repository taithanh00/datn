using datn.Data;
using datn.Middleware;
using datn.Models;
using datn.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Kết nối SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===== CẤU HÌNH JWT =====
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

// Authentication
builder.Services.AddAuthentication(options =>
{
    // Sử dụng JWT Bearer Authentication
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // TokenValidationParameters: Quy tắc để kiểm tra xem JWT có hợp lệ không
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,              // Kiểm tra người phát hành token
        ValidateAudience = true,            // Kiểm tra ai được dùng token
        ValidateLifetime = true,            // Kiểm tra token chưa hết hạn
        ValidateIssuerSigningKey = true,    // Kiểm tra chữ ký token (không bị giả mạo)
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
                                       Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero // Không cho phép trễ thêm giờ (token phải chính xác)
    };

    // JwtBearerEvents: Xử lý các sự kiện liên quan đến JWT
    options.Events = new JwtBearerEvents
    {
        // Bình thường JWT được gửi trong header "Authorization: Bearer <token>"
        // Tìm token trong cookie
        OnMessageReceived = context =>
        {
            context.Token = context.Request.Cookies["access_token"];
            return Task.CompletedTask;
        },

        // OnChallenge: Được gọi khi token không hợp lệ hoặc không tồn tại
        // Thay vì trả về 401, chúng ta chuyển hướng user tới trang Login
        OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.Redirect("/Auth/Login");
            return Task.CompletedTask;
        }
    };
});

// Thêm dịch vụ phân quyền (Authorization)
// Định nghĩa các policy cho từng vai trò
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ManagerOnly", p => p.RequireRole("Manager"));     // Chỉ Manager
    options.AddPolicy("EmployeeOnly", p => p.RequireRole("Employee"));   // Chỉ Employee
    options.AddPolicy("ParentOnly", p => p.RequireRole("Parent"));       // Chỉ Parent
});

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<JwtService>();                               // Dịch vụ tạo JWT
builder.Services.AddHostedService<TokenCleanupService>();               // Dịch vụ dọn dẹp token hết hạn

var app = builder.Build();

// ===== MIDDLEWARE =====
// Middleware xử lý refresh token tự động khi access token hết hạn
app.UseMiddleware<RefreshTokenMiddleware>();

// Phục vụ các file tĩnh (JS, CSS, ảnh từ wwwroot)
app.UseStaticFiles();

// Xác thực & Phân quyền (PHẢI có thứ tự này)
app.UseAuthentication();  // Xác thực user (kiểm tra JWT token)
app.UseAuthorization();   // Phân quyền (kiểm tra role/permission)

// Định tuyến controller
app.MapDefaultControllerRoute();
app.Run();