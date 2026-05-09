using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace datn.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherRolesAndLeadClass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SpecializedSubjects",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeacherType",
                table: "Employees",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LeadTeacherId",
                table: "Classes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Classes_LeadTeacherId",
                table: "Classes",
                column: "LeadTeacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Employees_LeadTeacherId",
                table: "Classes",
                column: "LeadTeacherId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Employees_LeadTeacherId",
                table: "Classes");

            migrationBuilder.DropIndex(
                name: "IX_Classes_LeadTeacherId",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "SpecializedSubjects",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "TeacherType",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "LeadTeacherId",
                table: "Classes");
        }
    }
}
