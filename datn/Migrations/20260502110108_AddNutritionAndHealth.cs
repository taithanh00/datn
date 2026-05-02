using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace datn.Migrations
{
    /// <inheritdoc />
    public partial class AddNutritionAndHealth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Allergies",
                table: "Students",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Temperature",
                table: "HealthRecords",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DailyReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    EatingStatus = table.Column<int>(type: "int", nullable: false),
                    EatingNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SleepingStatus = table.Column<int>(type: "int", nullable: false),
                    SleepingNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HygieneNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HealthNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActivityNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MoodNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhotoPaths = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyReports_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Menus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    MealType = table.Column<int>(type: "int", nullable: false),
                    DishName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ingredients = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Calories = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Menus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MenuId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: true),
                    ClassId = table.Column<int>(type: "int", nullable: true),
                    NewDishName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuOverrides_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MenuOverrides_Menus_MenuId",
                        column: x => x.MenuId,
                        principalTable: "Menus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MenuOverrides_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyReports_StudentId_Date",
                table: "DailyReports",
                columns: new[] { "StudentId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuOverrides_ClassId",
                table: "MenuOverrides",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuOverrides_MenuId",
                table: "MenuOverrides",
                column: "MenuId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuOverrides_StudentId",
                table: "MenuOverrides",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Menus_Date_MealType",
                table: "Menus",
                columns: new[] { "Date", "MealType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyReports");

            migrationBuilder.DropTable(
                name: "MenuOverrides");

            migrationBuilder.DropTable(
                name: "Menus");

            migrationBuilder.DropColumn(
                name: "Allergies",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "Temperature",
                table: "HealthRecords");
        }
    }
}
