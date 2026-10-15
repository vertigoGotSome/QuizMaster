using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizMaster.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddJeopardyDifficulty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Difficulty",
                table: "JeopardyQuestions",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.Sql(
                """
                UPDATE JeopardyQuestions
                SET Difficulty = CASE
                    WHEN BoardRound = 2 THEN
                        CASE WHEN CEILING(Value / 100.0) > 5 THEN 10 ELSE CEILING(Value / 100.0) + 5 END
                    ELSE
                        CASE WHEN CEILING(Value / 100.0) > 5 THEN 5 ELSE CEILING(Value / 100.0) END
                END
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Difficulty", table: "JeopardyQuestions");
        }
    }
}
