using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace datn.Migrations
{
    public partial class AddSalaryWorkflowFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BaseSalarySnapshot",
                table: "Salaries",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CoverageBonusAmount",
                table: "Salaries",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "CalculatedAtUtc",
                table: "Salaries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedAtUtc",
                table: "Salaries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAtUtc",
                table: "Salaries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Salaries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentNote",
                table: "Salaries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PenaltyAmount",
                table: "Salaries",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "StandardWorkingDays",
                table: "Salaries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Salaries",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Salaries_Status",
                table: "Salaries",
                column: "Status");

            migrationBuilder.Sql(@"
UPDATE s
SET
    s.Status = CASE WHEN pp.IsLocked = 1 THEN 2 ELSE 1 END,
    s.BaseSalarySnapshot = CASE
        WHEN (s.BaseSalarySnapshot IS NULL OR s.BaseSalarySnapshot = 0) AND COALESCE(e.BaseSalary, 0) > 0 THEN e.BaseSalary
        ELSE s.BaseSalarySnapshot
    END,
    s.StandardWorkingDays = COALESCE(s.StandardWorkingDays, 26),
    s.PenaltyAmount = COALESCE(s.PenaltyAmount, 0),
    s.CoverageBonusAmount = COALESCE(s.CoverageBonusAmount, 0),
    s.CalculatedAtUtc = COALESCE(s.CalculatedAtUtc, SYSUTCDATETIME()),
    s.LockedAtUtc = CASE
        WHEN pp.IsLocked = 1 THEN COALESCE(s.LockedAtUtc, pp.LockedAtUtc, SYSUTCDATETIME())
        ELSE s.LockedAtUtc
    END
FROM Salaries s
INNER JOIN PayrollPeriods pp ON pp.Id = s.PayrollPeriodId
INNER JOIN Employees e ON e.Id = s.EmployeeId;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Salaries_Status",
                table: "Salaries");

            migrationBuilder.DropColumn(
                name: "BaseSalarySnapshot",
                table: "Salaries");

            migrationBuilder.DropColumn(
                name: "CoverageBonusAmount",
                table: "Salaries");

            migrationBuilder.DropColumn(
                name: "CalculatedAtUtc",
                table: "Salaries");

            migrationBuilder.DropColumn(
                name: "LockedAtUtc",
                table: "Salaries");

            migrationBuilder.DropColumn(
                name: "PaidAtUtc",
                table: "Salaries");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Salaries");

            migrationBuilder.DropColumn(
                name: "PaymentNote",
                table: "Salaries");

            migrationBuilder.DropColumn(
                name: "PenaltyAmount",
                table: "Salaries");

            migrationBuilder.DropColumn(
                name: "StandardWorkingDays",
                table: "Salaries");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Salaries");
        }
    }
}
