using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizMaster.Data.Migrations;

/// <summary>
/// Ensures that the Sessions table contains the HostPlaysBlind column.
/// This setting allows the host to participate without revealing
/// the secret word in the host interface.
/// </summary>
public partial class AddMissingModelChanges : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add the missing column before updating its values.
        migrationBuilder.AddColumn<bool>(
            name: "HostPlaysBlind",
            table: "Sessions",
            type: "bit",
            nullable: false,
            defaultValue: false
        );

        // Enable blind mode for the existing default session.
        migrationBuilder.UpdateData(
            table: "Sessions",
            keyColumn: "Id",
            keyValue: 1,
            column: "HostPlaysBlind",
            value: true
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Remove the column when rolling back this migration.
        migrationBuilder.DropColumn(name: "HostPlaysBlind", table: "Sessions");
    }
}
