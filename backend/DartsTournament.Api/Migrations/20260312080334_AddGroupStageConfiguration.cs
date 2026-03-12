using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DartsTournament.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupStageConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasKnockoutPhase",
                table: "Tournaments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfGroups",
                table: "Tournaments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlayersPerGroup",
                table: "Tournaments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QualifiersPerGroup",
                table: "Tournaments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsKnockoutMatch",
                table: "Matches",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasKnockoutPhase",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "NumberOfGroups",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "PlayersPerGroup",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "QualifiersPerGroup",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "IsKnockoutMatch",
                table: "Matches");
        }
    }
}
