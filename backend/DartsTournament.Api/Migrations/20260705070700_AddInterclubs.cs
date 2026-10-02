using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DartsTournament.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInterclubs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "TournamentId",
                table: "TournamentTeams",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "EncounterId",
                table: "TournamentTeams",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "Players",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TournamentId",
                table: "Matches",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "EncounterId",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Clubs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clubs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InterclubChampionships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SinglesPerEncounter = table.Column<int>(type: "integer", nullable: false),
                    DoublesPerEncounter = table.Column<int>(type: "integer", nullable: false),
                    LegsToWin = table.Column<int>(type: "integer", nullable: false),
                    GameMode = table.Column<int>(type: "integer", nullable: false),
                    DoubleOut = table.Column<bool>(type: "boolean", nullable: false),
                    PointsForWin = table.Column<int>(type: "integer", nullable: false),
                    PointsForDraw = table.Column<int>(type: "integer", nullable: false),
                    PointsForLoss = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterclubChampionships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChampionshipClubs",
                columns: table => new
                {
                    ChampionshipId = table.Column<int>(type: "integer", nullable: false),
                    ClubId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChampionshipClubs", x => new { x.ChampionshipId, x.ClubId });
                    table.ForeignKey(
                        name: "FK_ChampionshipClubs_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChampionshipClubs_InterclubChampionships_ChampionshipId",
                        column: x => x.ChampionshipId,
                        principalTable: "InterclubChampionships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChampionshipRosterEntries",
                columns: table => new
                {
                    ChampionshipId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    ClubId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChampionshipRosterEntries", x => new { x.ChampionshipId, x.PlayerId });
                    table.ForeignKey(
                        name: "FK_ChampionshipRosterEntries_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChampionshipRosterEntries_InterclubChampionships_Championsh~",
                        column: x => x.ChampionshipId,
                        principalTable: "InterclubChampionships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChampionshipRosterEntries_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InterclubEncounters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChampionshipId = table.Column<int>(type: "integer", nullable: false),
                    Round = table.Column<int>(type: "integer", nullable: false),
                    HomeClubId = table.Column<int>(type: "integer", nullable: false),
                    AwayClubId = table.Column<int>(type: "integer", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    HomeScore = table.Column<int>(type: "integer", nullable: false),
                    AwayScore = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterclubEncounters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterclubEncounters_Clubs_AwayClubId",
                        column: x => x.AwayClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InterclubEncounters_Clubs_HomeClubId",
                        column: x => x.HomeClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InterclubEncounters_InterclubChampionships_ChampionshipId",
                        column: x => x.ChampionshipId,
                        principalTable: "InterclubChampionships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTeams_EncounterId",
                table: "TournamentTeams",
                column: "EncounterId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TournamentTeams_ExactlyOneParent",
                table: "TournamentTeams",
                sql: "(\"TournamentId\" IS NULL) <> (\"EncounterId\" IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_Players_ClubId",
                table: "Players",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_EncounterId",
                table: "Matches",
                column: "EncounterId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Matches_ExactlyOneParent",
                table: "Matches",
                sql: "(\"TournamentId\" IS NULL) <> (\"EncounterId\" IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_ChampionshipClubs_ClubId",
                table: "ChampionshipClubs",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_ChampionshipRosterEntries_ChampionshipId_ClubId",
                table: "ChampionshipRosterEntries",
                columns: new[] { "ChampionshipId", "ClubId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChampionshipRosterEntries_ClubId",
                table: "ChampionshipRosterEntries",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_ChampionshipRosterEntries_PlayerId",
                table: "ChampionshipRosterEntries",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_InterclubEncounters_AwayClubId",
                table: "InterclubEncounters",
                column: "AwayClubId");

            migrationBuilder.CreateIndex(
                name: "IX_InterclubEncounters_ChampionshipId",
                table: "InterclubEncounters",
                column: "ChampionshipId");

            migrationBuilder.CreateIndex(
                name: "IX_InterclubEncounters_HomeClubId",
                table: "InterclubEncounters",
                column: "HomeClubId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_InterclubEncounters_EncounterId",
                table: "Matches",
                column: "EncounterId",
                principalTable: "InterclubEncounters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Clubs_ClubId",
                table: "Players",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TournamentTeams_InterclubEncounters_EncounterId",
                table: "TournamentTeams",
                column: "EncounterId",
                principalTable: "InterclubEncounters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_InterclubEncounters_EncounterId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Players_Clubs_ClubId",
                table: "Players");

            migrationBuilder.DropForeignKey(
                name: "FK_TournamentTeams_InterclubEncounters_EncounterId",
                table: "TournamentTeams");

            migrationBuilder.DropTable(
                name: "ChampionshipClubs");

            migrationBuilder.DropTable(
                name: "ChampionshipRosterEntries");

            migrationBuilder.DropTable(
                name: "InterclubEncounters");

            migrationBuilder.DropTable(
                name: "Clubs");

            migrationBuilder.DropTable(
                name: "InterclubChampionships");

            migrationBuilder.DropIndex(
                name: "IX_TournamentTeams_EncounterId",
                table: "TournamentTeams");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TournamentTeams_ExactlyOneParent",
                table: "TournamentTeams");

            migrationBuilder.DropIndex(
                name: "IX_Players_ClubId",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Matches_EncounterId",
                table: "Matches");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Matches_ExactlyOneParent",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "EncounterId",
                table: "TournamentTeams");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "EncounterId",
                table: "Matches");

            migrationBuilder.AlterColumn<int>(
                name: "TournamentId",
                table: "TournamentTeams",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TournamentId",
                table: "Matches",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
