namespace DartsTournament.Api.Configuration;

/// <summary>
/// Habillage de l'instance (section "Branding" de la configuration,
/// surchargeable par variable d'environnement, ex. Branding__Skin=hacienda).
/// Les skins eux-mêmes (palette, polices, logo) sont définis côté frontend.
/// </summary>
public class BrandingOptions
{
    public const string SectionName = "Branding";

    /// <summary>Nom du skin frontend ("default", "hacienda"...). Un skin inconnu retombe sur le défaut.</summary>
    public string Skin { get; set; } = "default";

    /// <summary>Nom affiché dans la navbar et l'onglet ; vide = nom par défaut du skin</summary>
    public string? AppName { get; set; }
}
