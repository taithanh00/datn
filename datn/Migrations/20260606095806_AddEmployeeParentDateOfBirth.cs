using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace datn.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeParentDateOfBirth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfBirth",
                table: "Parents",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfBirth",
                table: "Employees",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Parents");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Employees");
        }
    }
}
