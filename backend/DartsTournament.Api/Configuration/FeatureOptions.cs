using DartsTournament.Api.Models;

namespace DartsTournament.Api.Configuration;

/// <summary>
/// Modules fonctionnels activables par instance (section "Features" de la configuration,
/// surchargeable par variable d'environnement, ex. Features__Interclubs=false).
/// Désactiver un module masque ses écrans et ses endpoints sans supprimer de données.
/// </summary>
public class FeatureOptions
{
    public const string SectionName = "Features";

    /// <summary>Circuits de tournois et classement à points</summary>
    public bool Circuits { get; set; } = true;

    /// <summary>Clubs, championnats interclubs et rencontres</summary>
    public bool Interclubs { get; set; } = true;

    /// <summary>Tournois en double (paires)</summary>
    public bool Doubles { get; set; } = true;

    /// <summary>Écran TV du bar (/tv)</summary>
    public bool Tv { get; set; } = true;

    /// <summary>Mode de jeu Cricket</summary>
    public bool Cricket { get; set; } = true;

    /// <summary>Mode de jeu 301</summary>
    public bool ThreeOhOne { get; set; } = true;

    /// <summary>Auto-inscription des joueurs aux tournois</summary>
    public bool OnlineRegistration { get; set; } = true;

    /// <summary>Saisons (mode Ligue : règlement 501 double out commun)</summary>
    public bool Seasons { get; set; } = true;

    /// <summary>Exploits (180, 171, checkouts de 100+, manches en 15 fléchettes ou moins) et leur classement de saison</summary>
    public bool Exploits { get; set; } = true;

    public bool IsEnabled(Feature feature) => feature switch
    {
        Feature.Circuits => Circuits,
        Feature.Interclubs => Interclubs,
        Feature.Doubles => Doubles,
        Feature.Tv => Tv,
        Feature.Cricket => Cricket,
        Feature.ThreeOhOne => ThreeOhOne,
        Feature.OnlineRegistration => OnlineRegistration,
        Feature.Seasons => Seasons,
        Feature.Exploits => Exploits,
        _ => throw new ArgumentOutOfRangeException(nameof(feature), feature, null)
    };

    /// <summary>
    /// Le 501 est toujours disponible ; Cricket et 301 dépendent de leur module.
    /// </summary>
    public bool IsGameModeEnabled(GameMode mode) => mode switch
    {
        GameMode.Cricket => Cricket,
        GameMode.ThreeOhOne => ThreeOhOne,
        _ => true
    };
}

public enum Feature
{
    Circuits,
    Interclubs,
    Doubles,
    Tv,
    Cricket,
    ThreeOhOne,
    OnlineRegistration,
    Seasons,
    Exploits
}
