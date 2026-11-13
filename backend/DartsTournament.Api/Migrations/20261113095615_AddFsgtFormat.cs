using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DartsTournament.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFsgtFormat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KnockoutPairings",
                table: "Tournaments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UseFsgtFormat",
                table: "Tournaments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "QualifierCount",
                table: "Groups",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KnockoutPairings",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "UseFsgtFormat",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "QualifierCount",
                table: "Groups");
        }
    }
}
