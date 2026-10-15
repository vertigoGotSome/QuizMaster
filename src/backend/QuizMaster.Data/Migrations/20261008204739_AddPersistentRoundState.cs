using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizMaster.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPersistentRoundState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TurnOrder",
                table: "Roles",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.CreateTable(
                name: "ImposterClues",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    Round = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    Pass = table.Column<int>(type: "int", nullable: false),
                    TurnIndex = table.Column<int>(type: "int", nullable: false),
                    Clue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImposterClues", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "QuestionUses",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    Round = table.Column<int>(type: "int", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionUses", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "RoundParticipants",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    Game = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Round = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoundParticipants", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_ImposterClues_SessionId_Round_TurnIndex",
                table: "ImposterClues",
                columns: new[] { "SessionId", "Round", "TurnIndex" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_QuestionUses_SessionId_QuestionId",
                table: "QuestionUses",
                columns: new[] { "SessionId", "QuestionId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_RoundParticipants_SessionId_Game_Round_PlayerId",
                table: "RoundParticipants",
                columns: new[] { "SessionId", "Game", "Round", "PlayerId" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ImposterClues");

            migrationBuilder.DropTable(name: "QuestionUses");

            migrationBuilder.DropTable(name: "RoundParticipants");

            migrationBuilder.DropColumn(name: "TurnOrder", table: "Roles");
        }
    }
}
