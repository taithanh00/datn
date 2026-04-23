using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace datn.Migrations
{
    /// <inheritdoc />
    public partial class AddRevokedAtUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RevokedAtUtc",
                table: "RefreshTokens",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                table: "ClassSchedules",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassSchedules_LocationId",
                table: "ClassSchedules",
                column: "LocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSchedules_Locations_LocationId",
                table: "ClassSchedules",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassSchedules_Locations_LocationId",
                table: "ClassSchedules");

            migrationBuilder.DropIndex(
                name: "IX_ClassSchedules_LocationId",
                table: "ClassSchedules");

            migrationBuilder.DropColumn(
                name: "RevokedAtUtc",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "ClassSchedules");
        }
    }
}
