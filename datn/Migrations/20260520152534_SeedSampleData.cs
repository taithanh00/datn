using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace datn.Migrations
{
    /// <inheritdoc />
    public partial class SeedSampleData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var hash = "$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcg7b3XeKeUxWdeS86E36P4/tvQe"; // 123456
            var now = new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc);

            for (int i = 1; i <= 10; i++)
            {
                // Accounts (Teacher)
                migrationBuilder.InsertData(
                    table: "Accounts",
                    columns: new[] { "Id", "Username", "PasswordHash", "PasswordSalt", "Email", "IsActive", "RoleId", "CreatedAt", "UpdatedAt" },
                    values: new object[] { 1000 + i, $"teacher{i}", hash, "", $"teacher{i}@test.com", true, 2, now, now });

                // Employees (Teacher)
                migrationBuilder.InsertData(
                    table: "Employees",
                    columns: new[] { "Id", "AccountId", "FirstName", "LastName", "Phone", "Position", "BaseSalary", "IsActive", "Gender" },
                    values: new object[] { 1000 + i, 1000 + i, $"Giáo Viên {i}", "Nguyễn", $"0900000{i:D3}", "Giáo viên", 10000000m, true, true });

                // Accounts (Parent)
                migrationBuilder.InsertData(
                    table: "Accounts",
                    columns: new[] { "Id", "Username", "PasswordHash", "PasswordSalt", "Email", "IsActive", "RoleId", "CreatedAt", "UpdatedAt" },
                    values: new object[] { 2000 + i, $"parent{i}", hash, "", $"parent{i}@test.com", true, 3, now, now });

                // Parents
                migrationBuilder.InsertData(
                    table: "Parents",
                    columns: new[] { "Id", "AccountId", "FirstName", "LastName", "Phone", "Address", "IsActive", "Gender" },
                    values: new object[] { 2000 + i, 2000 + i, $"Phụ Huynh {i}", "Trần", $"0910000{i:D3}", $"Địa chỉ {i}", true, true });
            }

            // Classes
            for (int i = 1; i <= 5; i++)
            {
                migrationBuilder.InsertData(
                    table: "Classes",
                    columns: new[] { "Id", "Name", "AgeFrom", "AgeTo", "SchoolYear", "MaxCapacity", "IsActive", "LeadTeacherId" },
                    values: new object[] { 1000 + i, $"Lớp Mẫu Giáo {i}", 3, 5, "2025-2026", 30, true, 1000 + i });
                
                // Assign teacher to class (Lead teacher)
                migrationBuilder.InsertData(
                    table: "Assignments",
                    columns: new[] { "EmployeeId", "ClassId", "StartDate", "EndDate", "RoleInClass", "IsActive" },
                    values: new object[] { 1000 + i, 1000 + i, new DateOnly(2025, 1, 1), new DateOnly(2026, 12, 31), "Chu Nhiem", true });
            }

            // Students
            for (int i = 1; i <= 10; i++)
            {
                int classId = 1000 + ((i - 1) % 5 + 1);
                migrationBuilder.InsertData(
                    table: "Students",
                    columns: new[] { "Id", "StudentCode", "ClassId", "FirstName", "LastName", "Gender", "DateOfBirth", "EnrollDate", "Status", "CreatedAt" },
                    values: new object[] { 1000 + i, $"HS{i:D4}", classId, $"Học Sinh {i}", "Lê", true, new DateOnly(2020, 1, i), new DateOnly(2025, 9, 1), 0, now }); 

                // Link Parent to Student
                migrationBuilder.InsertData(
                    table: "ParentStudents",
                    columns: new[] { "ParentId", "StudentId", "Relationship" },
                    values: new object[] { 2000 + i, 1000 + i, "Bố" });
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            for (int i = 1; i <= 10; i++)
            {
                migrationBuilder.DeleteData(table: "ParentStudents", keyColumns: new[] { "ParentId", "StudentId" }, keyValues: new object[] { 2000 + i, 1000 + i });
                migrationBuilder.DeleteData(table: "Students", keyColumn: "Id", keyValue: 1000 + i);
            }

            for (int i = 1; i <= 5; i++)
            {
                migrationBuilder.DeleteData(table: "Assignments", keyColumns: new[] { "EmployeeId", "ClassId", "StartDate" }, keyValues: new object[] { 1000 + i, 1000 + i, new DateOnly(2025, 1, 1) });
                migrationBuilder.DeleteData(table: "Classes", keyColumn: "Id", keyValue: 1000 + i);
            }

            for (int i = 1; i <= 10; i++)
            {
                migrationBuilder.DeleteData(table: "Parents", keyColumn: "Id", keyValue: 2000 + i);
                migrationBuilder.DeleteData(table: "Accounts", keyColumn: "Id", keyValue: 2000 + i);
                migrationBuilder.DeleteData(table: "Employees", keyColumn: "Id", keyValue: 1000 + i);
                migrationBuilder.DeleteData(table: "Accounts", keyColumn: "Id", keyValue: 1000 + i);
            }
        }
    }
}
