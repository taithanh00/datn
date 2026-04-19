using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace datn.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckInCheckOutFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "WorkAttendances",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckInAtUtc",
                table: "WorkAttendances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckOutAtUtc",
                table: "WorkAttendances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLate",
                table: "WorkAttendances",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PenaltyAmount",
                table: "WorkAttendances",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ReviewNote",
                table: "WorkAttendances",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAtUtc",
                table: "WorkAttendances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewedByEmployeeId",
                table: "WorkAttendances",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WorkUnit",
                table: "WorkAttendances",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkedMinutes",
                table: "WorkAttendances",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckInAtUtc",
                table: "WorkAttendances");

            migrationBuilder.DropColumn(
                name: "CheckOutAtUtc",
                table: "WorkAttendances");

            migrationBuilder.DropColumn(
                name: "IsLate",
                table: "WorkAttendances");

            migrationBuilder.DropColumn(
                name: "PenaltyAmount",
                table: "WorkAttendances");

            migrationBuilder.DropColumn(
                name: "ReviewNote",
                table: "WorkAttendances");

            migrationBuilder.DropColumn(
                name: "ReviewedAtUtc",
                table: "WorkAttendances");

            migrationBuilder.DropColumn(
                name: "ReviewedByEmployeeId",
                table: "WorkAttendances");

            migrationBuilder.DropColumn(
                name: "WorkUnit",
                table: "WorkAttendances");

            migrationBuilder.DropColumn(
                name: "WorkedMinutes",
                table: "WorkAttendances");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "WorkAttendances",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
