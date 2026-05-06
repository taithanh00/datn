using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace datn.Migrations
{
    /// <inheritdoc />
    public partial class SplitEmployeeName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "Employees",
                newName: "LastName");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            // Tách dữ liệu từ LastName (cũ là FullName) sang FirstName và LastName mới
            // Ưu tiên lấy từ cuối cùng làm FirstName, phần còn lại là LastName
            migrationBuilder.Sql(@"
                UPDATE Employees 
                SET FirstName = LTRIM(RIGHT(LastName, CHARINDEX(' ', REVERSE(LastName) + ' ') - 1)),
                    LastName = LTRIM(LEFT(LastName, LEN(LastName) - CHARINDEX(' ', REVERSE(LastName) + ' ') + 1))
                WHERE LastName IS NOT NULL AND LastName <> '';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Employees");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "Employees",
                newName: "FullName");
        }
    }
}
