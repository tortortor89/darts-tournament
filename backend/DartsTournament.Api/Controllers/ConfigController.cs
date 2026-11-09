using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using DartsTournament.Api.Configuration;

namespace DartsTournament.Api.Controllers;

/// <summary>
/// Configuration publique de l'instance (modules activés, habillage)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ConfigController : ControllerBase
{
    private readonly IOptionsMonitor<FeatureOptions> _features;
    private readonly IOptionsMonitor<BrandingOptions> _branding;

    public ConfigController(IOptionsMonitor<FeatureOptions> features, IOptionsMonitor<BrandingOptions> branding)
    {
        _features = features;
        _branding = branding;
    }

    /// <summary>
    /// Récupérer les modules activés sur cette instance
    /// </summary>
    [HttpGet("features")]
    [ProducesResponseType(typeof(FeatureOptions), StatusCodes.Status200OK)]
    public ActionResult<FeatureOptions> GetFeatures()
    {
        return Ok(_features.CurrentValue);
    }

    /// <summary>
    /// Récupérer l'habillage (skin) de cette instance
    /// </summary>
    [HttpGet("branding")]
    [ProducesResponseType(typeof(BrandingOptions), StatusCodes.Status200OK)]
    public ActionResult<BrandingOptions> GetBranding()
    {
        return Ok(_branding.CurrentValue);
    }
}
