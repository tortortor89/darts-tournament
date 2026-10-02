using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DartsTournament.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixTeamSizeDefaultAndLegacyStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "TeamSize",
                table: "Tournaments",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "integer");

            // Réparation de données : AddDoublesSupport avait rempli TeamSize avec 0
            // pour les tournois existants (tous en simple)
            migrationBuilder.Sql("""
                UPDATE "Tournaments" SET "TeamSize" = 1 WHERE "TeamSize" = 0;
                """);

            // Réparation de données : la migration AddRegistrationStatus avait rempli
            // les inscriptions antérieures avec Pending (0). Un joueur d'un tournoi déjà
            // démarré ou terminé est de facto approuvé — les vraies inscriptions en
            // attente ne concernent que les tournois en Draft (statut 0)
            migrationBuilder.Sql("""
                UPDATE "TournamentPlayers" tp
                SET "Status" = 1
                FROM "Tournaments" t
                WHERE tp."TournamentId" = t."Id"
                  AND tp."Status" = 0
                  AND t."Status" <> 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "TeamSize",
                table: "Tournaments",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1);
        }
    }
}
