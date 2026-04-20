using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DartsTournament.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackDoublesToMatchSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TrackDoubles",
                table: "MatchSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrackDoubles",
                table: "MatchSessions");
        }
    }
}
