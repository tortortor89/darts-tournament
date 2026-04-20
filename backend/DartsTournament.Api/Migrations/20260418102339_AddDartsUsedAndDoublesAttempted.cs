using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DartsTournament.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDartsUsedAndDoublesAttempted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DartsUsed",
                table: "Throws",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DoublesAttempted",
                table: "Throws",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DartsUsed",
                table: "Throws");

            migrationBuilder.DropColumn(
                name: "DoublesAttempted",
                table: "Throws");
        }
    }
}
