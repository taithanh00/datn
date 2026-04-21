using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace datn.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAccountUniqueAndTeacherFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "TeachingPlans",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Curriculums",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubjectId",
                table: "Curriculums",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Accounts",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Accounts",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "StudentActivities",
                columns: table => new
                {
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentActivities", x => new { x.StudentId, x.ActivityId });
                    table.ForeignKey(
                        name: "FK_StudentActivities_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentActivities_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Curriculums_SubjectId",
                table: "Curriculums",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Email",
                table: "Accounts",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Username",
                table: "Accounts",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentActivities_ActivityId",
                table: "StudentActivities",
                column: "ActivityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Curriculums_Subjects_SubjectId",
                table: "Curriculums",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Curriculums_Subjects_SubjectId",
                table: "Curriculums");

            migrationBuilder.DropTable(
                name: "StudentActivities");

            migrationBuilder.DropIndex(
                name: "IX_Curriculums_SubjectId",
                table: "Curriculums");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_Email",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_Username",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TeachingPlans");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Curriculums");

            migrationBuilder.DropColumn(
                name: "SubjectId",
                table: "Curriculums");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Accounts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Accounts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
