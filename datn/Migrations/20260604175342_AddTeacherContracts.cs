using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace datn.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherContracts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeacherContracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ContractNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ContractType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SignedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AgreedSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    WorkPosition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkLocation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkingHours = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TerminationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TerminationReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OriginalFileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StoredFileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    UploadedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherContracts", x => x.Id);
                    table.CheckConstraint("CK_TeacherContracts_ExpiryDate", "[ExpiryDate] IS NULL OR [ExpiryDate] >= [EffectiveDate]");
                    table.ForeignKey(
                        name: "FK_TeacherContracts_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherContracts_ContractNumber",
                table: "TeacherContracts",
                column: "ContractNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherContracts_EmployeeId_Status",
                table: "TeacherContracts",
                columns: new[] { "EmployeeId", "Status" },
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherContracts_ExpiryDate",
                table: "TeacherContracts",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherContracts_Status",
                table: "TeacherContracts",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeacherContracts");
        }
    }
}
