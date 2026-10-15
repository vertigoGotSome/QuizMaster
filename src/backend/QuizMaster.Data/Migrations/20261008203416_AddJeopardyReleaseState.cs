using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizMaster.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddJeopardyReleaseState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "JeopardyQuestionId",
                table: "Sessions",
                type: "int",
                nullable: true
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "TimerEndsUtc",
                table: "Sessions",
                type: "datetime2",
                nullable: true
            );

            migrationBuilder.CreateTable(
                name: "JeopardyPlays",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    ContestantId = table.Column<int>(type: "int", nullable: true),
                    StealContestantId = table.Column<int>(type: "int", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScoreDelta = table.Column<int>(type: "int", nullable: false),
                    StealScoreDelta = table.Column<int>(type: "int", nullable: false),
                    OpenedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JeopardyPlays", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "JeopardyQuestions",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Board = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BoardRound = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<int>(type: "int", nullable: false),
                    Clue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AcceptedAnswers = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HostNotes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JeopardyQuestions", x => x.Id);
                }
            );

            migrationBuilder.UpdateData(
                table: "Sessions",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "JeopardyQuestionId", "TimerEndsUtc" },
                values: new object[] { null, null }
            );

            migrationBuilder.CreateIndex(
                name: "IX_JeopardyPlays_SessionId_QuestionId",
                table: "JeopardyPlays",
                columns: new[] { "SessionId", "QuestionId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_JeopardyQuestions_Board_BoardRound_Category_Value",
                table: "JeopardyQuestions",
                columns: new[] { "Board", "BoardRound", "Category", "Value" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "JeopardyPlays");

            migrationBuilder.DropTable(name: "JeopardyQuestions");

            migrationBuilder.DropColumn(name: "JeopardyQuestionId", table: "Sessions");

            migrationBuilder.DropColumn(name: "TimerEndsUtc", table: "Sessions");
        }
    }
}
