using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizMaster.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialQuizMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Answers",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    Round = table.Column<int>(type: "int", nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrectOverride = table.Column<bool>(type: "bit", nullable: true),
                    Joker = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Answers", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "JokerUses",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Round = table.Column<int>(type: "int", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JokerUses", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DiscordId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AvatarUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsHost = table.Column<bool>(type: "bit", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    Team = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastSeenUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Difficulty = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnswerA = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnswerB = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnswerC = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnswerD = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Correct = table.Column<string>(type: "nvarchar(max)", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    Round = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    IsImposter = table.Column<bool>(type: "bit", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "ScoreEvents",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    Round = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    Delta = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreEvents", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Game = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phase = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Round = table.Column<int>(type: "int", nullable: false),
                    HelperWords = table.Column<bool>(type: "bit", nullable: false),
                    ImposterCount = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: true),
                    WordId = table.Column<int>(type: "int", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    QuestionIndex = table.Column<int>(type: "int", nullable: false),
                    Scored = table.Column<bool>(type: "bit", nullable: false),
                    SpeakerIndex = table.Column<int>(type: "int", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Votes",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    Round = table.Column<int>(type: "int", nullable: false),
                    VoterId = table.Column<int>(type: "int", nullable: false),
                    SuspectId = table.Column<int>(type: "int", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Votes", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Words",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Word = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Hint = table.Column<string>(type: "nvarchar(max)", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Words", x => x.Id);
                }
            );

            migrationBuilder.InsertData(
                table: "Sessions",
                columns: new[]
                {
                    "Id",
                    "Game",
                    "HelperWords",
                    "ImposterCount",
                    "Phase",
                    "QuestionId",
                    "QuestionIndex",
                    "Round",
                    "Scored",
                    "SpeakerIndex",
                    "Version",
                    "WordId",
                },
                values: new object[] { 1, "none", true, 1, "lobby", null, 0, 0, false, 0, 1, null }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Answers_SessionId_PlayerId_Round",
                table: "Answers",
                columns: new[] { "SessionId", "PlayerId", "Round" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_JokerUses_SessionId_PlayerId_Kind",
                table: "JokerUses",
                columns: new[] { "SessionId", "PlayerId", "Kind" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Players_DiscordId",
                table: "Players",
                column: "DiscordId",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Roles_SessionId_Round_PlayerId",
                table: "Roles",
                columns: new[] { "SessionId", "Round", "PlayerId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Votes_SessionId_Round_VoterId",
                table: "Votes",
                columns: new[] { "SessionId", "Round", "VoterId" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Answers");

            migrationBuilder.DropTable(name: "JokerUses");

            migrationBuilder.DropTable(name: "Players");

            migrationBuilder.DropTable(name: "Questions");

            migrationBuilder.DropTable(name: "Roles");

            migrationBuilder.DropTable(name: "ScoreEvents");

            migrationBuilder.DropTable(name: "Sessions");

            migrationBuilder.DropTable(name: "Votes");

            migrationBuilder.DropTable(name: "Words");
        }
    }
}
