using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DartsTournament.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDoubleElimination : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowBracketReset",
                table: "Tournaments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "BracketType",
                table: "Matches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsBracketReset",
                table: "Matches",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowBracketReset",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "BracketType",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "IsBracketReset",
                table: "Matches");
        }
    }
}
