using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DartsTournament.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSeasons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SeasonId",
                table: "Tournaments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SeasonId",
                table: "InterclubChampionships",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SeasonId",
                table: "Circuits",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Seasons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LegsToWin = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seasons", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_SeasonId",
                table: "Tournaments",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_InterclubChampionships_SeasonId",
                table: "InterclubChampionships",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_Circuits_SeasonId",
                table: "Circuits",
                column: "SeasonId");

            migrationBuilder.AddForeignKey(
                name: "FK_Circuits_Seasons_SeasonId",
                table: "Circuits",
                column: "SeasonId",
                principalTable: "Seasons",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InterclubChampionships_Seasons_SeasonId",
                table: "InterclubChampionships",
                column: "SeasonId",
                principalTable: "Seasons",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Tournaments_Seasons_SeasonId",
                table: "Tournaments",
                column: "SeasonId",
                principalTable: "Seasons",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Circuits_Seasons_SeasonId",
                table: "Circuits");

            migrationBuilder.DropForeignKey(
                name: "FK_InterclubChampionships_Seasons_SeasonId",
                table: "InterclubChampionships");

            migrationBuilder.DropForeignKey(
                name: "FK_Tournaments_Seasons_SeasonId",
                table: "Tournaments");

            migrationBuilder.DropTable(
                name: "Seasons");

            migrationBuilder.DropIndex(
                name: "IX_Tournaments_SeasonId",
                table: "Tournaments");

            migrationBuilder.DropIndex(
                name: "IX_InterclubChampionships_SeasonId",
                table: "InterclubChampionships");

            migrationBuilder.DropIndex(
                name: "IX_Circuits_SeasonId",
                table: "Circuits");

            migrationBuilder.DropColumn(
                name: "SeasonId",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "SeasonId",
                table: "InterclubChampionships");

            migrationBuilder.DropColumn(
                name: "SeasonId",
                table: "Circuits");
        }
    }
}
