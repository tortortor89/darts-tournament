using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace DartsTournament.Api.Configuration;

/// <summary>
/// Répond 404 si le module n'est pas activé sur cette instance
/// (applicable à un contrôleur ou à une action).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequiresFeatureAttribute : Attribute, IResourceFilter
{
    public Feature Feature { get; }

    public RequiresFeatureAttribute(Feature feature)
    {
        Feature = feature;
    }

    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var features = context.HttpContext.RequestServices
            .GetRequiredService<IOptionsMonitor<FeatureOptions>>().CurrentValue;

        if (!features.IsEnabled(Feature))
        {
            context.Result = new NotFoundObjectResult(new { message = "Fonctionnalité non disponible" });
        }
    }

    public void OnResourceExecuted(ResourceExecutedContext context) { }
}
