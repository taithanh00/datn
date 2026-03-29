using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace datn.Migrations
{
    /// <inheritdoc />
    public partial class SeedTestAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Hash password "123456" using BCrypt
            // Password: 123456
            // BCrypt Hash: $2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcg7b3XeKeUxWdeS86E36P4/tvQe

            // Seed Employee Account
            migrationBuilder.InsertData(
                table: "Accounts",
                columns: new[] { "Username", "PasswordHash", "PasswordSalt", "Email", "IsActive", "RoleId", "CreatedAt", "UpdatedAt" },
                values: new object[] { 
                    "giaoviengiaovien", 
                    "$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcg7b3XeKeUxWdeS86E36P4/tvQe",
                    "",
                    "giaoviengiaovien@kindergarten.edu.vn",
                    true,
                    2,  // Employee role
                    new DateTime(2025, 3, 22, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2025, 3, 22, 0, 0, 0, DateTimeKind.Utc)
                });

            // Seed Parent Account
            migrationBuilder.InsertData(
                table: "Accounts",
                columns: new[] { "Username", "PasswordHash", "PasswordSalt", "Email", "IsActive", "RoleId", "CreatedAt", "UpdatedAt" },
                values: new object[] { 
                    "phuhuynh1", 
                    "$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcg7b3XeKeUxWdeS86E36P4/tvQe",
                    "",
                    "phuhuynh1@kindergarten.edu.vn",
                    true,
                    3,  // Parent role
                    new DateTime(2025, 3, 22, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2025, 3, 22, 0, 0, 0, DateTimeKind.Utc)
                });

            // Seed Employee record linked to account
            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "AccountId", "FullName", "Phone", "Position", "BaseSalary" },
                values: new object[] {
                    1,  // AccountId (from previous insert)
                    "Nguyễn Văn Giáo Viên",
                    "0912345678",
                    "Giáo viên mầm non",
                    15000000.00m
                });

            // Seed Parent record linked to account
            migrationBuilder.InsertData(
                table: "Parents",
                columns: new[] { "AccountId", "FirstName", "LastName", "Phone", "Address" },
                values: new object[] {
                    2,  // AccountId (from previous insert)
                    "Nguyễn",
                    "Phụ Huynh",
                    "0987654321",
                    "123 Đường Nguyễn Huệ, TP.HCM"
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Delete test data
            migrationBuilder.DeleteData(
                table: "Parents",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Accounts",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Accounts",
                keyColumn: "Id",
                keyValue: 2);
        }
    }
}