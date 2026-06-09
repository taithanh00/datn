using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace datn.Migrations
{
    /// <inheritdoc />
    public partial class ImportStudentData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Students",
                columns: new[] { "Id", "FirstName", "LastName", "Gender", "DateOfBirth", "Address", "ClassId", "EnrollDate", "AvatarPath", "Allergies", "Status", "CreatedAt" },
                values: new object[,]
                {
                    { 3001, "Gia Bách", "Bùi", true, new DateOnly(2021, 10, 2), "45 Lý Thánh Tôn, Nha Trang, Khánh Hòa", 1, new DateOnly(2024, 9, 5), "/images/lion_orange.png", "Không", 0, new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc) },
                    { 3002, "Ngọc Linh Đan", "Lê", false, new DateOnly(2022, 9, 5), "Thôn Diên Điền, Diên Khánh, Khánh Hòa", 1, new DateOnly(2024, 9, 5), "/images/lion_orange.png", "Không", 0, new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc) },
                    { 3003, "Duy Khang", "Nguyễn", true, new DateOnly(2021, 12, 10), "Thôn Diên Sơn, Diên Khánh, Khánh Hòa", 1, new DateOnly(2024, 9, 5), "/images/lion_orange.png", "Không", 0, new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc) },
                    { 3004, "Hoàng Bảo Nhi", "Tăng", false, new DateOnly(2022, 11, 20), "Thôn Diên Điền, Diên Khánh, Khánh Hòa", 1, new DateOnly(2024, 9, 5), "/images/lion_orange.png", "Không", 0, new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc) },
                    { 3005, "Hải Đăng", "Trần", true, new DateOnly(2021, 12, 9), "Thôn Diên Phước, Diên Khánh, Khánh Hòa", 1, new DateOnly(2024, 9, 5), "/images/lion_orange.png", "Không", 0, new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc) },
                    { 3006, "Hoàng Gia Hân", "Lê", false, new DateOnly(2021, 10, 2), "Thôn Diên Sơn, Diên Khánh, Khánh Hòa", 2, new DateOnly(2024, 9, 5), "/images/lion_orange.png", "Không", 0, new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc) },
                    { 3007, "Nhật Minh", "Lê", true, new DateOnly(2020, 11, 9), "Xã Vĩnh Thạnh, Nha Trang, Khánh Hòa", 2, new DateOnly(2024, 9, 5), "/images/lion_orange.png", "Không", 0, new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc) },
                    { 3008, "Quốc Phúc", "Trần", true, new DateOnly(2021, 11, 15), "Thôn Diên Lạc, Diên Khánh, Khánh Hòa", 2, new DateOnly(2024, 9, 5), "/images/lion_orange.png", "Không", 0, new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc) },
                    { 3009, "Gia Vỹ", "Đào", true, new DateOnly(2020, 12, 28), "Thôn Diên Điền, Diên Khánh, Khánh Hòa", 2, new DateOnly(2024, 9, 5), "/images/lion_orange.png", "Không", 0, new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc) },
                    { 3010, "Huỳnh Bảo Ngọc", "Phạm", false, new DateOnly(2021, 1, 2), "Thôn Diên Phước, Diên Khánh, Khánh Hòa", 2, new DateOnly(2024, 9, 5), "/images/lion_orange.png", "Không", 0, new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc) },
                    { 3011, "Võ Trà My", "Lê", false, new DateOnly(2020, 1, 4), "Thôn Diên An, Diên Khánh, Khánh Hòa", 3, new DateOnly(2024, 9, 5), "/images/lion_orange.png", "Không", 0, new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc) },
                    { 3012, "Phúc Chi", "Nguyễn", false, new DateOnly(2019, 12, 2), "Thôn Diên Sơn, Diên Khánh, Khánh Hòa", 3, new DateOnly(2024, 9, 5), "/images/lion_orange.png", "Không", 0, new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc) },
                    { 3013, "Trí Nguyên", "Nguyễn", true, new DateOnly(2020, 6, 12), "Thôn Diên Phước, Diên Khánh, Khánh Hòa", 3, new DateOnly(2024, 9, 5), "/images/lion_orange.png", "Không", 0, new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc) },
                    { 3014, "Nguyễn Minh Khang", "Lê", true, new DateOnly(2019, 11, 2), "Thôn Võ Cang, Nha Trang, Khánh Hòa", 3, new DateOnly(2024, 9, 5), "/images/lion_orange.png", "Không", 0, new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc) },
                    { 3015, "Hoàng Quân", "Phan", true, new DateOnly(2020, 7, 2), "Thôn Diên Sơn, Diên Khánh, Khánh Hòa", 3, new DateOnly(2024, 9, 5), "/images/lion_orange.png", "Không", 0, new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc) },
                    { 3016, "Thiên Bảo", "Trần", true, new DateOnly(2019, 9, 25), "Thôn Diên Sơn, Diên Khánh, Khánh Hòa", 4, new DateOnly(2024, 9, 5), "/images/lion_orange.png", "Không", 0, new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc) },
                    { 3017, "Nhật Hoàng Hạ", "Trần", false, new DateOnly(2018, 11, 15), "Thôn Diên An, Diên Khánh, Khánh Hòa", 4, new DateOnly(2024, 9, 5), "/images/lion_orange.png", "Không", 0, new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc) },
                    { 3018, "Nguyễn Phương Quỳnh", "Lê", false, new DateOnly(2019, 5, 27), "Thôn Diên Lạc, Diên Khánh, Khánh Hòa", 4, new DateOnly(2024, 9, 5), "/images/lion_orange.png", "Không", 0, new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc) },
                    { 3019, "Ngọc Thiên Kim", "Nguyễn", false, new DateOnly(2018, 10, 9), "Thôn Diên Phước, Diên Khánh, Khánh Hòa", 4, new DateOnly(2024, 9, 5), "/images/lion_orange.png", "Không", 0, new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc) },
                    { 3020, "Thành Luân", "Phan", true, new DateOnly(2019, 8, 19), "Thôn Diên Sơn, Diên Khánh, Khánh Hòa", 4, new DateOnly(2024, 9, 5), "/images/lion_orange.png", "Không", 0, new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            for (int i = 3001; i <= 3020; i++)
            {
                migrationBuilder.DeleteData(
                    table: "Students",
                    keyColumn: "Id",
                    keyValue: i);
            }
        }
    }
}
