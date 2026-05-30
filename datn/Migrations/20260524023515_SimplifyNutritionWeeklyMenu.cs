using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace datn.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyNutritionWeeklyMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Menus_Date_MealType",
                table: "Menus");

            migrationBuilder.AddColumn<int>(
                name: "DayOfWeek",
                table: "Menus",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE [Menus]
                SET [DayOfWeek] = (DATEDIFF(day, CONVERT(date, '00010101', 112), [Date]) % 7) + 1;
                """);

            migrationBuilder.Sql("""
                WITH RankedMenus AS (
                    SELECT
                        [Id],
                        ROW_NUMBER() OVER (
                            PARTITION BY [DayOfWeek], [MealType]
                            ORDER BY [Date] DESC, [Id] DESC
                        ) AS [RowNumber]
                    FROM [Menus]
                    WHERE [IsActive] = 1 AND [DayOfWeek] BETWEEN 1 AND 5
                )
                UPDATE [Menus]
                SET [IsActive] = 0
                WHERE [Id] IN (
                    SELECT [Id]
                    FROM RankedMenus
                    WHERE [RowNumber] > 1
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Menus_DayOfWeek_MealType",
                table: "Menus",
                columns: new[] { "DayOfWeek", "MealType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Menus_DayOfWeek_MealType",
                table: "Menus");

            migrationBuilder.DropColumn(
                name: "DayOfWeek",
                table: "Menus");

            migrationBuilder.CreateIndex(
                name: "IX_Menus_Date_MealType",
                table: "Menus",
                columns: new[] { "Date", "MealType" });
        }
    }
}
