using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DartsTournament.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatchId = table.Column<int>(type: "integer", nullable: false),
                    LegsToWin = table.Column<int>(type: "integer", nullable: false),
                    GameMode = table.Column<int>(type: "integer", nullable: false),
                    StartingPlayerId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Player1LegsWon = table.Column<int>(type: "integer", nullable: false),
                    Player2LegsWon = table.Column<int>(type: "integer", nullable: false),
                    Player1CurrentScore = table.Column<int>(type: "integer", nullable: false),
                    Player2CurrentScore = table.Column<int>(type: "integer", nullable: false),
                    CurrentLeg = table.Column<int>(type: "integer", nullable: false),
                    CurrentPlayerId = table.Column<int>(type: "integer", nullable: false),
                    CurrentLegStartingPlayerId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchSessions_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Throws",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatchSessionId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    LegNumber = table.Column<int>(type: "integer", nullable: false),
                    ThrowNumber = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Dart1 = table.Column<string>(type: "text", nullable: true),
                    Dart2 = table.Column<string>(type: "text", nullable: true),
                    Dart3 = table.Column<string>(type: "text", nullable: true),
                    RemainingScore = table.Column<int>(type: "integer", nullable: false),
                    IsCheckout = table.Column<bool>(type: "boolean", nullable: false),
                    IsBust = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Throws", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Throws_MatchSessions_MatchSessionId",
                        column: x => x.MatchSessionId,
                        principalTable: "MatchSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Throws_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchSessions_MatchId",
                table: "MatchSessions",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Throws_MatchSessionId",
                table: "Throws",
                column: "MatchSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Throws_PlayerId",
                table: "Throws",
                column: "PlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Throws");

            migrationBuilder.DropTable(
                name: "MatchSessions");
        }
    }
}
