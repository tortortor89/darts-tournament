namespace DartsTournament.Api.Services;

public record BoardLineupInput(int Position, IReadOnlyList<int> HomePlayerIds, IReadOnlyList<int> AwayPlayerIds);

/// <summary>
/// Validation de la composition d'une rencontre interclubs.
/// Boards 1..S = simples (1 joueur par côté), S+1..S+D = doubles (2 joueurs par côté).
/// Les joueurs doivent appartenir à l'effectif déclaré du bon club. Un même joueur
/// peut jouer plusieurs boards (ex: un simple + un double) — décision v1.
/// Fonction pure, retourne la liste des erreurs en français (vide si valide).
/// </summary>
public static class EncounterLineupValidator
{
    public static List<string> Validate(
        IReadOnlyList<BoardLineupInput> boards,
        int singlesCount,
        int doublesCount,
        IReadOnlySet<int> homeRosterIds,
        IReadOnlySet<int> awayRosterIds)
    {
        var errors = new List<string>();
        int totalBoards = singlesCount + doublesCount;

        var duplicatePositions = boards
            .GroupBy(b => b.Position)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        foreach (var pos in duplicatePositions)
            errors.Add($"Le board {pos} est défini plusieurs fois");

        foreach (var board in boards)
        {
            if (board.Position < 1 || board.Position > totalBoards)
            {
                errors.Add($"Board {board.Position} invalide : la rencontre compte {totalBoards} matchs ({singlesCount} simples + {doublesCount} doubles)");
                continue;
            }

            bool isDoubleBoard = board.Position > singlesCount;
            int expected = isDoubleBoard ? 2 : 1;
            string boardLabel = isDoubleBoard
                ? $"Board {board.Position} (double)"
                : $"Board {board.Position} (simple)";

            if (board.HomePlayerIds.Count != expected)
                errors.Add($"{boardLabel} : {expected} joueur(s) attendu(s) côté domicile, {board.HomePlayerIds.Count} fourni(s)");
            if (board.AwayPlayerIds.Count != expected)
                errors.Add($"{boardLabel} : {expected} joueur(s) attendu(s) côté extérieur, {board.AwayPlayerIds.Count} fourni(s)");

            if (isDoubleBoard)
            {
                if (board.HomePlayerIds.Count == 2 && board.HomePlayerIds[0] == board.HomePlayerIds[1])
                    errors.Add($"{boardLabel} : la paire domicile doit être composée de deux joueurs différents");
                if (board.AwayPlayerIds.Count == 2 && board.AwayPlayerIds[0] == board.AwayPlayerIds[1])
                    errors.Add($"{boardLabel} : la paire extérieure doit être composée de deux joueurs différents");
            }

            foreach (var playerId in board.HomePlayerIds.Where(p => !homeRosterIds.Contains(p)))
                errors.Add($"{boardLabel} : le joueur {playerId} ne fait pas partie de l'effectif du club domicile");
            foreach (var playerId in board.AwayPlayerIds.Where(p => !awayRosterIds.Contains(p)))
                errors.Add($"{boardLabel} : le joueur {playerId} ne fait pas partie de l'effectif du club extérieur");
        }

        return errors;
    }
}
