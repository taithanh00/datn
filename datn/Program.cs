using datn.Data;
using datn.Hubs;
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
        },

        // OnForbidden: Được gọi khi user đã được xác thực nhưng không có quyền truy cập (403)
        // Chuyển hướng user tới trang AccessDenied
        OnForbidden = context =>
        {
            context.Response.Redirect("/Auth/AccessDenied");
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

// Cấu hình đường dẫn khi truy cập bị từ chối (403)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/Auth/AccessDenied";
});

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddScoped<JwtService>();                               // Dịch vụ tạo JWT
builder.Services.AddScoped<INotificationService, NotificationService>();   // Dịch vụ thông báo
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IParentService, ParentService>();           // Dịch vụ quản lý học sinh
builder.Services.AddHostedService<TokenCleanupService>();               // Dịch vụ dọn dẹp token hết hạn
builder.Services.AddHostedService<PayrollAutoCalculationService>();     // Tự động tính lương ngày 5 hàng tháng

var app = builder.Build();

// 1. Phục vụ file tĩnh (CSS, JS, Images) ngay lập tức - Sửa lỗi UI
app.UseStaticFiles();

// 2. Định tuyến
app.UseRouting();

// 3. Xác thực người dùng (Lấy thông tin từ Cookie/Token)
app.UseAuthentication();

// 4. Middleware kiểm tra Token và Bắt đổi mật khẩu
app.UseMiddleware<RefreshTokenMiddleware>();

// 5. Kiểm tra quyền truy cập
app.UseAuthorization();

// Map route
app.MapDefaultControllerRoute();
app.MapHub<RealtimeHub>("/hubs/realtime");
app.Run();