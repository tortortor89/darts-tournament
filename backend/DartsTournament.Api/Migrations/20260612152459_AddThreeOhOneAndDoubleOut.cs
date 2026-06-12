using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DartsTournament.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddThreeOhOneAndDoubleOut : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Les sessions x01 existantes étaient toutes en double out
            migrationBuilder.AddColumn<bool>(
                name: "DoubleOut",
                table: "MatchSessions",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // GameMode.Cricket passe de 301 à 1 (301 devient le mode ThreeOhOne)
            migrationBuilder.Sql("""UPDATE "MatchSessions" SET "GameMode" = 1 WHERE "GameMode" = 301;""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""UPDATE "MatchSessions" SET "GameMode" = 301 WHERE "GameMode" = 1;""");

            migrationBuilder.DropColumn(
                name: "DoubleOut",
                table: "MatchSessions");
        }
    }
}
