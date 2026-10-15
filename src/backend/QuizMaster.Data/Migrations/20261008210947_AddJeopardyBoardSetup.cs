using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizMaster.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddJeopardyBoardSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JeopardyCategoriesJson",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<int>(
                name: "JeopardyRound",
                table: "Sessions",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.UpdateData(
                table: "Sessions",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "JeopardyCategoriesJson", "JeopardyRound" },
                values: new object[] { "[]", 1 }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "JeopardyCategoriesJson", table: "Sessions");

            migrationBuilder.DropColumn(name: "JeopardyRound", table: "Sessions");
        }
    }
}
