using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizMaster.Data.Migrations;

/// <summary>Adds the per-session host secrecy option without deleting existing content.</summary>
public partial class HostBlindMode : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "HostPlaysBlind",
            table: "Sessions",
            type: "bit",
            nullable: false,
            defaultValue: true
        );
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "HostPlaysBlind", table: "Sessions");
    }
}
