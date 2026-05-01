using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace datn.Migrations
{
    /// <inheritdoc />
    public partial class UnifiedFeeModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tuitions_TuitionPlans_TuitionPlanId",
                table: "Tuitions");

            migrationBuilder.DropTable(
                name: "TuitionPlans");

            migrationBuilder.DropIndex(
                name: "IX_Tuitions_TuitionPlanId",
                table: "Tuitions");

            migrationBuilder.DropColumn(
                name: "TuitionPlanId",
                table: "Tuitions");

            migrationBuilder.AddColumn<int>(
                name: "AgeFrom",
                table: "FeeItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AgeTo",
                table: "FeeItems",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgeFrom",
                table: "FeeItems");

            migrationBuilder.DropColumn(
                name: "AgeTo",
                table: "FeeItems");

            migrationBuilder.AddColumn<int>(
                name: "TuitionPlanId",
                table: "Tuitions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TuitionPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgeFrom = table.Column<int>(type: "int", nullable: true),
                    AgeTo = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TuitionPlans", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tuitions_TuitionPlanId",
                table: "Tuitions",
                column: "TuitionPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tuitions_TuitionPlans_TuitionPlanId",
                table: "Tuitions",
                column: "TuitionPlanId",
                principalTable: "TuitionPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
