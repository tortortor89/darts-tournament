using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DartsTournament.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCircuits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CircuitId",
                table: "Tournaments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Circuits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ParticipationPoints = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Circuits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CircuitPointsRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CircuitId = table.Column<int>(type: "integer", nullable: false),
                    MinRank = table.Column<int>(type: "integer", nullable: false),
                    MaxRank = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CircuitPointsRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CircuitPointsRules_Circuits_CircuitId",
                        column: x => x.CircuitId,
                        principalTable: "Circuits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_CircuitId",
                table: "Tournaments",
                column: "CircuitId");

            migrationBuilder.CreateIndex(
                name: "IX_CircuitPointsRules_CircuitId",
                table: "CircuitPointsRules",
                column: "CircuitId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tournaments_Circuits_CircuitId",
                table: "Tournaments",
                column: "CircuitId",
                principalTable: "Circuits",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tournaments_Circuits_CircuitId",
                table: "Tournaments");

            migrationBuilder.DropTable(
                name: "CircuitPointsRules");

            migrationBuilder.DropTable(
                name: "Circuits");

            migrationBuilder.DropIndex(
                name: "IX_Tournaments_CircuitId",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "CircuitId",
                table: "Tournaments");
        }
    }
}
