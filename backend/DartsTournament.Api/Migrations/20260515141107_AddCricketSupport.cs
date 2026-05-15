using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DartsTournament.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCricketSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CricketDataJson",
                table: "Throws",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CricketStateJson",
                table: "MatchSessions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CricketDataJson",
                table: "Throws");

            migrationBuilder.DropColumn(
                name: "CricketStateJson",
                table: "MatchSessions");
        }
    }
}
