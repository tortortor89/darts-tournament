using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DartsTournament.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDoublesSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TeamSize",
                table: "Tournaments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Side1Player1Id",
                table: "MatchSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Side1Player2Id",
                table: "MatchSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Side2Player1Id",
                table: "MatchSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Side2Player2Id",
                table: "MatchSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Team1Id",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Team2Id",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WinnerTeamId",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TournamentTeams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TournamentId = table.Column<int>(type: "integer", nullable: false),
                    Player1Id = table.Column<int>(type: "integer", nullable: false),
                    Player2Id = table.Column<int>(type: "integer", nullable: false),
                    Seed = table.Column<int>(type: "integer", nullable: true),
                    GroupId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentTeams_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TournamentTeams_Players_Player1Id",
                        column: x => x.Player1Id,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentTeams_Players_Player2Id",
                        column: x => x.Player2Id,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentTeams_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Matches_Team1Id",
                table: "Matches",
                column: "Team1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_Team2Id",
                table: "Matches",
                column: "Team2Id");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_WinnerTeamId",
                table: "Matches",
                column: "WinnerTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTeams_GroupId",
                table: "TournamentTeams",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTeams_Player1Id",
                table: "TournamentTeams",
                column: "Player1Id");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTeams_Player2Id",
                table: "TournamentTeams",
                column: "Player2Id");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTeams_TournamentId_Player1Id",
                table: "TournamentTeams",
                columns: new[] { "TournamentId", "Player1Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTeams_TournamentId_Player2Id",
                table: "TournamentTeams",
                columns: new[] { "TournamentId", "Player2Id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_TournamentTeams_Team1Id",
                table: "Matches",
                column: "Team1Id",
                principalTable: "TournamentTeams",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_TournamentTeams_Team2Id",
                table: "Matches",
                column: "Team2Id",
                principalTable: "TournamentTeams",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_TournamentTeams_WinnerTeamId",
                table: "Matches",
                column: "WinnerTeamId",
                principalTable: "TournamentTeams",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_TournamentTeams_Team1Id",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_TournamentTeams_Team2Id",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_TournamentTeams_WinnerTeamId",
                table: "Matches");

            migrationBuilder.DropTable(
                name: "TournamentTeams");

            migrationBuilder.DropIndex(
                name: "IX_Matches_Team1Id",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_Team2Id",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_WinnerTeamId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "TeamSize",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "Side1Player1Id",
                table: "MatchSessions");

            migrationBuilder.DropColumn(
                name: "Side1Player2Id",
                table: "MatchSessions");

            migrationBuilder.DropColumn(
                name: "Side2Player1Id",
                table: "MatchSessions");

            migrationBuilder.DropColumn(
                name: "Side2Player2Id",
                table: "MatchSessions");

            migrationBuilder.DropColumn(
                name: "Team1Id",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Team2Id",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "WinnerTeamId",
                table: "Matches");
        }
    }
}
