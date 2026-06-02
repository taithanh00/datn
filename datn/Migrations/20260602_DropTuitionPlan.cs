using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace datn.Migrations
{
    public partial class DropTuitionPlan : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Tuitions_TuitionPlans_TuitionPlanId')
    ALTER TABLE [Tuitions] DROP CONSTRAINT [FK_Tuitions_TuitionPlans_TuitionPlanId];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tuitions_TuitionPlanId' AND object_id = OBJECT_ID('Tuitions'))
    DROP INDEX [IX_Tuitions_TuitionPlanId] ON [Tuitions];
IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'TuitionPlanId' AND Object_ID = Object_ID(N'Tuitions'))
    ALTER TABLE [Tuitions] DROP COLUMN [TuitionPlanId];
IF OBJECT_ID('TuitionPlans','U') IS NOT NULL
    DROP TABLE [TuitionPlans];");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TuitionPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgeFrom = table.Column<int>(type: "int", nullable: true),
                    AgeTo = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TuitionPlans", x => x.Id);
                });

            migrationBuilder.AddColumn<int>(
                name: "TuitionPlanId",
                table: "Tuitions",
                type: "int",
                nullable: true);

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
