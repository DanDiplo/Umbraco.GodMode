using Diplo.GodMode.Models;
using Diplo.GodMode.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Services;

namespace Diplo.GodMode.Services;

public class DeliveryApiDiagnosticsService : IDeliveryApiDiagnosticsService
{
    private static readonly string[] SensitiveAliasTerms = ["account", "auth", "config", "customer", "member", "profile", "secret", "secure", "setting", "user"];

    private readonly IConfiguration configuration;
    private readonly IContentTypeService contentTypeService;
    private readonly ILanguageService languageService;
    private readonly IOptions<DeliveryApiSettings> deliveryApiSettings;

    public DeliveryApiDiagnosticsService(
        IConfiguration configuration,
        IContentTypeService contentTypeService,
        ILanguageService languageService,
        IOptions<DeliveryApiSettings> deliveryApiSettings)
    {
        this.configuration = configuration;
        this.contentTypeService = contentTypeService;
        this.languageService = languageService;
        this.deliveryApiSettings = deliveryApiSettings;
    }

    public async Task<DeliveryApiDiagnostics> GetDiagnosticsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var settings = deliveryApiSettings.Value;
        var enabled = GetBoolSetting(settings, "Enabled", "Umbraco:CMS:DeliveryApi:Enabled");
        var publicAccess = GetBoolSetting(settings, "PublicAccess", "Umbraco:CMS:DeliveryApi:PublicAccess");
        var apiKeyConfigured = !string.IsNullOrWhiteSpace(configuration["Umbraco:CMS:DeliveryApi:ApiKey"]);
        var disallowedAliases = GetDisallowedContentTypeAliases(settings).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var contentTypes = contentTypeService.GetAll()
            .Where(contentType => !contentType.IsElement)
            .OrderBy(contentType => contentType.Name)
            .Select(contentType =>
            {
                var isDisallowed = disallowedAliases.Contains(contentType.Alias);
                return new DeliveryApiContentTypeExposure
                {
                    Name = contentType.Name ?? contentType.Alias,
                    Alias = contentType.Alias,
                    Key = contentType.Key,
                    IsElement = contentType.IsElement,
                    IsExposed = enabled && !isDisallowed,
                    Exposure = !enabled ? "Delivery API disabled" : isDisallowed ? "Disallowed" : "Exposed by default",
                    SensitiveAlias = IsSensitiveAlias(contentType.Alias)
                };
            })
            .ToList();

        var diagnostics = new DeliveryApiDiagnostics
        {
            Enabled = enabled,
            PublicAccess = publicAccess,
            ApiKeyConfigured = apiKeyConfigured,
            DisallowedContentTypeAliases = disallowedAliases.OrderBy(x => x),
            AvailableCultures = (await languageService.GetAllAsync()).Select(x => x.IsoCode).OrderBy(x => x),
            SampleEndpoints =
            [
                "/umbraco/delivery/api/v2/content",
                "/umbraco/delivery/api/v2/content/item/{id-or-path}",
                "/umbraco/delivery/api/v2/content?fetch=children:{id-or-path}",
                "/umbraco/delivery/api/v2/content?fields=properties[alias1,alias2]",
                "/umbraco/delivery/api/v2/content?expand=properties[alias]"
            ],
            ContentTypes = contentTypes
        };

        diagnostics.Findings = BuildFindings(diagnostics);

        return diagnostics;
    }

    private IEnumerable<HealthRiskFinding> BuildFindings(DeliveryApiDiagnostics diagnostics)
    {
        if (!diagnostics.Enabled)
        {
            yield return CreateFinding(
                "Info",
                "Content Delivery API",
                "Content Delivery API is disabled",
                "The Content Delivery API is not enabled for this site.",
                "Delivery API",
                "Delivery API",
                string.Empty,
                string.Empty,
                "Enable it only if this site intentionally exposes content as JSON for headless or decoupled consumers.");

            yield break;
        }

        if (diagnostics.PublicAccess)
        {
            yield return CreateFinding(
                "Medium",
                "Content Delivery API",
                "Content Delivery API is publicly accessible",
                "Published content is available through the Delivery API without an API key unless content type aliases are disallowed.",
                "Delivery API",
                "Public access",
                string.Empty,
                string.Empty,
                "Confirm this is intentional and add sensitive content type aliases to DisallowedContentTypeAliases.");
        }

        if (!diagnostics.PublicAccess && !diagnostics.ApiKeyConfigured)
        {
            yield return CreateFinding(
                "High",
                "Content Delivery API",
                "Delivery API requires an API key but none is configured",
                "Public access is disabled, but no Delivery API key was found in configuration.",
                "Delivery API",
                "API key",
                string.Empty,
                string.Empty,
                "Configure Umbraco:CMS:DeliveryApi:ApiKey or re-enable public access if appropriate.");
        }

        if (!diagnostics.DisallowedContentTypeAliases.Any())
        {
            yield return CreateFinding(
                diagnostics.PublicAccess ? "Medium" : "Low",
                "Content Delivery API",
                "No disallowed content type aliases configured",
                "All published document types are eligible for Delivery API responses by default.",
                "Delivery API",
                "Disallowed aliases",
                string.Empty,
                string.Empty,
                "Review document types and configure DisallowedContentTypeAliases for anything that should never be returned by the API.");
        }

        foreach (var contentType in diagnostics.ContentTypes.Where(x => x.IsExposed && x.SensitiveAlias))
        {
            yield return CreateFinding(
                diagnostics.PublicAccess ? "High" : "Medium",
                "Content Delivery API",
                "Sensitive-looking document type is exposed",
                $"{contentType.Name} ({contentType.Alias}) matches a sensitive alias pattern and is eligible for Delivery API responses.",
                "Document Type",
                contentType.Name,
                contentType.Alias,
                contentType.Key.ToString(),
                "Add this alias to DisallowedContentTypeAliases unless it is deliberately part of the public API.");
        }
    }

    private bool GetBoolSetting(DeliveryApiSettings settings, string propertyName, string configurationKey)
    {
        var property = settings.GetType().GetProperty(propertyName);
        if (property?.GetValue(settings) is bool optionValue)
        {
            return optionValue;
        }

        return bool.TryParse(configuration[configurationKey], out var configurationValue) && configurationValue;
    }

    private IEnumerable<string> GetDisallowedContentTypeAliases(DeliveryApiSettings settings)
    {
        var property = settings.GetType().GetProperty("DisallowedContentTypeAliases");
        if (property?.GetValue(settings) is IEnumerable<string> optionValues)
        {
            return optionValues.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim());
        }

        return configuration.GetSection("Umbraco:CMS:DeliveryApi:DisallowedContentTypeAliases")
            .Get<string[]>()?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim()) ?? [];
    }

    private static bool IsSensitiveAlias(string alias)
        => SensitiveAliasTerms.Any(term => alias.Contains(term, StringComparison.OrdinalIgnoreCase));

    private static HealthRiskFinding CreateFinding(string severity, string category, string title, string detail, string entityType, string entityName, string entityAlias, string entityKey, string recommendation)
        => new()
        {
            Severity = severity,
            Score = severity switch
            {
                "High" => 80,
                "Medium" => 50,
                "Low" => 20,
                _ => 10
            },
            Category = category,
            Title = title,
            Detail = detail,
            EntityType = entityType,
            EntityName = entityName,
            EntityAlias = entityAlias,
            EntityKey = entityKey,
            Recommendation = recommendation
        };
}
