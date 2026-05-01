using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace datn.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultRankings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Rankings",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Đạt" },
                    { 2, "Cần cố gắng hơn" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Rankings",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Rankings",
                keyColumn: "Id",
                keyValue: 2);
        }
    }
}
