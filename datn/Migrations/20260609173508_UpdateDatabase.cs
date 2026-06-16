using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace datn.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM [sys].[indexes]
                    WHERE [name] = N'IX_Salaries_Status'
                        AND [object_id] = OBJECT_ID(N'[Salaries]')
                )
                BEGIN
                    DROP INDEX [IX_Salaries_Status] ON [Salaries];
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[Salaries]') IS NOT NULL
                    AND NOT EXISTS (
                        SELECT 1
                        FROM [sys].[indexes]
                        WHERE [name] = N'IX_Salaries_Status'
                            AND [object_id] = OBJECT_ID(N'[Salaries]')
                    )
                BEGIN
                    CREATE INDEX [IX_Salaries_Status] ON [Salaries] ([Status]);
                END
                """);
        }
    }
}
