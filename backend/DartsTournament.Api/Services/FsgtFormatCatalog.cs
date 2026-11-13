using System.Text.Json;
using System.Text.Json.Serialization;

namespace DartsTournament.Api.Services;

/// <summary>
/// Grille FSGT : format de tableau (poules imposées, qualifiés, affiches) selon le nombre
/// d'inscrits. Chargée depuis Data/fsgt-formats.json (généré par tools/fsgt, retouchable à la main).
/// </summary>
public class FsgtFormatCatalog
{
    public const string DefaultFileName = "fsgt-formats.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly Dictionary<int, FsgtFormatVariant> _defaults;

    // Départage par défaut si la grille n'en précise pas (choix du 2026-11-11)
    private static readonly TieBreaker[] DefaultTieBreakers =
        { TieBreaker.Wins, TieBreaker.LegDifference, TieBreaker.HeadToHead };

    public FsgtFormatCatalog(FsgtFormatFile file)
    {
        TieBreakers = file.TieBreakers is { Count: > 0 } ? file.TieBreakers : DefaultTieBreakers;
        Points = file.Points ?? throw new InvalidOperationException("Grille FSGT : barème « points » manquant");

        _defaults = new Dictionary<int, FsgtFormatVariant>();
        foreach (var format in file.Formats)
        {
            var variant = format.Variants.FirstOrDefault(v => v.Default) ?? format.Variants.FirstOrDefault();
            if (variant == null)
                continue;

            Validate(format.Players, variant);
            _defaults[format.Players] = variant;
        }

        MinPlayers = _defaults.Count > 0 ? _defaults.Keys.Min() : 0;
        MaxPlayers = _defaults.Count > 0 ? _defaults.Keys.Max() : 0;
    }

    public int MinPlayers { get; }
    public int MaxPlayers { get; }

    /// <summary>Critères de départage en poule, dans l'ordre, avant l'ordre manuel de l'admin.</summary>
    public IReadOnlyList<TieBreaker> TieBreakers { get; }

    /// <summary>Barème « Point Tournois » (option des circuits).</summary>
    public FsgtPoints Points { get; }

    public static FsgtFormatCatalog Load(string path)
    {
        using var stream = File.OpenRead(path);
        var file = JsonSerializer.Deserialize<FsgtFormatFile>(stream, JsonOptions)
            ?? throw new InvalidOperationException($"Grille FSGT illisible : {path}");
        return new FsgtFormatCatalog(file);
    }

    /// <summary>Variante par défaut pour ce nombre de côtés (joueurs ou paires), null hors grille.</summary>
    public FsgtFormatVariant? GetDefault(int sideCount) =>
        _defaults.TryGetValue(sideCount, out var variant) ? variant : null;

    private static void Validate(int players, FsgtFormatVariant variant)
    {
        int total = variant.Groups.Sum(g => g.Size);
        if (total != players)
            throw new InvalidOperationException(
                $"Grille FSGT, {players} joueurs : les poules totalisent {total} joueurs");

        int qualifiers = variant.Groups.Sum(g => g.Qualifiers);
        if (qualifiers != variant.Bracket.Count * 2)
            throw new InvalidOperationException(
                $"Grille FSGT, {players} joueurs : {qualifiers} qualifiés pour {variant.Bracket.Count} affiches");

        var slots = new HashSet<(int Rank, int Group)>();
        foreach (var pairing in variant.Bracket)
        {
            if (pairing.Count != 2)
                throw new InvalidOperationException($"Grille FSGT, {players} joueurs : affiche invalide");

            foreach (var token in pairing)
            {
                var slot = FsgtDrawCalculator.ParseSlot(token);
                if (slot.Group < 1 || slot.Group > variant.Groups.Count
                    || slot.Rank < 1 || slot.Rank > variant.Groups[slot.Group - 1].Qualifiers
                    || !slots.Add(slot))
                    throw new InvalidOperationException(
                        $"Grille FSGT, {players} joueurs : affiche « {token} » invalide ou en double");
            }
        }

        if (variant.Bracket.Count > 0 && (variant.Bracket.Count & (variant.Bracket.Count - 1)) != 0)
            throw new InvalidOperationException(
                $"Grille FSGT, {players} joueurs : {variant.Bracket.Count} affiches (puissance de 2 attendue)");
    }
}

public record FsgtFormatFile(
    List<FsgtFormatEntry> Formats,
    List<TieBreaker>? TieBreakers = null,
    FsgtPoints? Points = null);

/// <summary>
/// Barème FSGT : éliminés en poules = participation + points par victoire en poule ;
/// qualifiés = points du tour atteint, selon le nombre d'inscrits.
/// </summary>
public record FsgtPoints(FsgtPoolPoints PoolElimination, List<FsgtKnockoutPoints> Knockout);

public record FsgtPoolPoints(int Participation, int PerWin);

public record FsgtKnockoutPoints(
    int MinPlayers,
    int MaxPlayers,
    int? RoundOf32 = null,
    int? RoundOf16 = null,
    int? QuarterFinal = null,
    int? SemiFinal = null,
    int? Final = null,
    int? Winner = null);

public record FsgtFormatEntry(int Players, List<FsgtFormatVariant> Variants);

public record FsgtFormatVariant(
    bool Default,
    string? Knockout,
    List<FsgtGroupSpec> Groups,
    List<List<string>> Bracket)
{
    [JsonIgnore]
    public bool HasKnockout => Bracket.Count > 0;
}

public record FsgtGroupSpec(int Size, int Qualifiers);
