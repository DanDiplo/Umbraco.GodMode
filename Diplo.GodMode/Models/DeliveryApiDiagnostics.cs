namespace Diplo.GodMode.Models;

public class DeliveryApiDiagnostics
{
    public bool Enabled { get; set; }

    public bool PublicAccess { get; set; }

    public bool ApiKeyConfigured { get; set; }

    public IEnumerable<string> DisallowedContentTypeAliases { get; set; } = [];

    public IEnumerable<string> AvailableCultures { get; set; } = [];

    public IEnumerable<string> SampleEndpoints { get; set; } = [];

    public IEnumerable<DeliveryApiContentTypeExposure> ContentTypes { get; set; } = [];

    public IEnumerable<HealthRiskFinding> Findings { get; set; } = [];
}

public class DeliveryApiContentTypeExposure
{
    public string Name { get; set; } = string.Empty;

    public string Alias { get; set; } = string.Empty;

    public Guid Key { get; set; }

    public bool IsElement { get; set; }

    public bool IsExposed { get; set; }

    public string Exposure { get; set; } = string.Empty;

    public bool SensitiveAlias { get; set; }
}
