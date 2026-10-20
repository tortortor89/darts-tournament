using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using DartsTournament.Api.Configuration;

namespace DartsTournament.Api.Controllers;

/// <summary>
/// Configuration publique de l'instance (modules activés)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ConfigController : ControllerBase
{
    private readonly IOptionsMonitor<FeatureOptions> _features;

    public ConfigController(IOptionsMonitor<FeatureOptions> features)
    {
        _features = features;
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
}
