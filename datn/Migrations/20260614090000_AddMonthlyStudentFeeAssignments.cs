using System;
using datn.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace datn.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260614090000_AddMonthlyStudentFeeAssignments")]
    public partial class AddMonthlyStudentFeeAssignments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Salaries_Status",
                table: "Salaries");

            migrationBuilder.CreateTable(
                name: "MonthlyStudentFeeAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    FeeItemId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyStudentFeeAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyStudentFeeAssignments_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonthlyStudentFeeAssignments_FeeItems_FeeItemId",
                        column: x => x.FeeItemId,
                        principalTable: "FeeItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonthlyStudentFeeAssignments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyStudentFeeAssignments_ClassId",
                table: "MonthlyStudentFeeAssignments",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyStudentFeeAssignments_FeeItemId",
                table: "MonthlyStudentFeeAssignments",
                column: "FeeItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyStudentFeeAssignments_StudentId",
                table: "MonthlyStudentFeeAssignments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyStudentFeeAssignments_Month_Year_ClassId_StudentId_FeeItemId",
                table: "MonthlyStudentFeeAssignments",
                columns: new[] { "Month", "Year", "ClassId", "StudentId", "FeeItemId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonthlyStudentFeeAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_Salaries_Status",
                table: "Salaries",
                column: "Status");
        }
    }
}
