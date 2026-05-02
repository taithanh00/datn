using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace datn.Migrations
{
    /// <inheritdoc />
    public partial class AddGenderToParent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Gender",
                table: "Parents",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Parents");
        }
    }
}
