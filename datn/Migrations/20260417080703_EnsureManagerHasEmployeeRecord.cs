using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace datn.Migrations
{
    /// <inheritdoc />
    public partial class EnsureManagerHasEmployeeRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tự động chèn bản ghi Employee cho những Account có RoleId = 1 (Manager) nhưng chưa có bản ghi trong bảng Employees
            migrationBuilder.Sql(@"
                INSERT INTO Employees (AccountId, FullName, Phone, Position, BaseSalary)
                SELECT a.Id, a.Username, '0000000000', N'Quản lý hệ thống', 0
                FROM Accounts a
                LEFT JOIN Employees e ON a.Id = e.AccountId
                WHERE a.RoleId = 1 AND e.Id IS NULL
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Không thực hiện xóa vì có thể ảnh hưởng đến dữ liệu thực tế
        }
    }
}
