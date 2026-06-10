using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Asp.Versioning;
using Diplo.GodMode.Helpers;
using Diplo.GodMode.Models;
using Diplo.GodMode.Services;
using Diplo.GodMode.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NPoco;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Cms.Web.Website.Controllers;
using Umbraco.Extensions;

namespace Diplo.GodMode.Controllers;

/// <summary>
/// Management API for GodMode. Replaces the v13 UmbracoAuthorizedJsonController.
/// All endpoints sit under /umbraco/management/api/v1/godmode/...
/// </summary>
[ApiVersion("1.0")]
[VersionedApiBackOfficeRoute("godmode")]
[ApiExplorerSettings(GroupName = "GodMode")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class GodModeApiController : ManagementApiControllerBase
{
    private readonly IUmbracoDataService dataService;
    private readonly IUmbracoDatabaseService dataBaseService;
    private readonly IDiagnosticService diagnosticService;
    private readonly IGodModeHealthRiskService healthRiskService;
    private readonly IUtilitiesService utilitiesService;
    private readonly IDeliveryApiDiagnosticsService deliveryApiDiagnosticsService;
    private readonly IGodModeLogService logService;
    private readonly IHostApplicationLifetime applicationLifetime;
    private readonly NuCacheSettings nuCacheSettings;
    private readonly RegisteredServiceCollection registeredServiceCollection;
    private readonly IOptions<GodModeConfig> godModeConfig;

    public GodModeApiController(
        IUmbracoDataService dataService,
        IUmbracoDatabaseService dataBaseService,
        IDiagnosticService diagnosticService,
        IGodModeHealthRiskService healthRiskService,
        IUtilitiesService utilitiesService,
        IDeliveryApiDiagnosticsService deliveryApiDiagnosticsService,
        IGodModeLogService logService,
        IHostApplicationLifetime applicationLifetime,
        IOptions<NuCacheSettings> nuCacheSettings,
        RegisteredServiceCollection registeredServiceCollection,
        IOptions<GodModeConfig> godModeConfig)
    {
        this.dataService = dataService;
        this.dataBaseService = dataBaseService;
        this.diagnosticService = diagnosticService;
        this.healthRiskService = healthRiskService;
        this.utilitiesService = utilitiesService;
        this.deliveryApiDiagnosticsService = deliveryApiDiagnosticsService;
        this.logService = logService;
        this.applicationLifetime = applicationLifetime;
        this.nuCacheSettings = nuCacheSettings.Value;
        this.registeredServiceCollection = registeredServiceCollection;
        this.godModeConfig = godModeConfig;
    }

    // ─── Doc / content / data types ─────────────────────────────────

    [HttpGet("content-type-map")]
    [ProducesResponseType<IEnumerable<ContentTypeMap>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<ContentTypeMap>> GetContentTypeMap()
        => Ok(dataService.GetContentTypeMap());

    [HttpGet("property-groups")]
    [ProducesResponseType<IEnumerable<string>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<string>> GetPropertyGroups()
        => Ok(dataService.GetPropertyGroups());

    [HttpGet("compositions")]
    [ProducesResponseType<IEnumerable<ContentTypeCompositionData>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<ContentTypeCompositionData>> GetCompositions()
        => Ok(dataService.GetCompositions());

    [HttpGet("data-types")]
    [ProducesResponseType<IEnumerable<DataTypeMap>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DataTypeMap>>> GetDataTypes()
        => Ok(await dataService.GetDataTypes());

    [HttpGet("property-editors")]
    [ProducesResponseType<IEnumerable<DataTypeMap>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DataTypeMap>>> GetPropertyEditors()
        => Ok(await dataService.GetPropertyEditors());

    [HttpGet("data-types-status")]
    [ProducesResponseType<IEnumerable<DataTypeMap>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DataTypeMap>>> GetDataTypesStatus()
        => Ok(await dataService.GetDataTypesStatus());

    [HttpGet("reference-graph")]
    [ProducesResponseType<IEnumerable<ReferenceEdge>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ReferenceEdge>>> GetReferenceGraph()
        => Ok(await dataService.GetReferenceGraph());

    [HttpGet("references/used-by")]
    [ProducesResponseType<IEnumerable<ReferenceEdge>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ReferenceEdge>>> GetUsedBy([FromQuery] string targetType, [FromQuery] string targetKey)
        => Ok(await dataService.GetUsedBy(targetType, targetKey));

    [HttpGet("references/uses")]
    [ProducesResponseType<IEnumerable<ReferenceEdge>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ReferenceEdge>>> GetUses([FromQuery] string sourceType, [FromQuery] string sourceKey)
        => Ok(await dataService.GetUses(sourceType, sourceKey));

    [HttpGet("configuration-drift-findings")]
    [ProducesResponseType<IEnumerable<ConfigurationDriftFinding>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ConfigurationDriftFinding>>> GetConfigurationDriftFindings()
        => Ok(await dataService.GetConfigurationDriftFindings());

    [HttpGet("health-risk-findings")]
    [ProducesResponseType<IEnumerable<HealthRiskFinding>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<HealthRiskFinding>>> GetHealthRiskFindings()
        => Ok(await healthRiskService.BuildHealthRiskFindingsAsync(HttpContext.RequestAborted));

    [HttpGet("delivery-api/diagnostics")]
    [ProducesResponseType<DeliveryApiDiagnostics>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DeliveryApiDiagnostics>> GetDeliveryApiDiagnostics()
        => Ok(await deliveryApiDiagnosticsService.GetDiagnosticsAsync(HttpContext.RequestAborted));

    [HttpGet("templates")]
    [ProducesResponseType<IEnumerable<TemplateModel>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TemplateModel>>> GetTemplates()
        => Ok(await dataService.GetTemplates());

    // ─── Content / media / members ───────────────────────────────────

    [HttpGet("media")]
    [ProducesResponseType<Page<MediaMap>>(StatusCodes.Status200OK)]
    public ActionResult<Page<MediaMap>> GetMedia(
        long page = 1,
        int pageSize = 3,
        string? name = null,
        int? id = null,
        int? mediaTypeId = null,
        long? minSizeBytes = null,
        string orderBy = "Id",
        string orderByDir = "ASC")
        => Ok(dataService.GetMediaPaged(page, pageSize, name, id, mediaTypeId, minSizeBytes, orderBy, orderByDir));

    [HttpGet("media/{id:int}/detail")]
    [ProducesResponseType<ContentMediaDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentMediaDetail>> GetMediaDetail(int id)
    {
        var detail = await dataService.GetMediaDetail(id);

        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpGet("media-types")]
    [ProducesResponseType<IEnumerable<ItemBase>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<ItemBase>> GetMediaTypes()
        => Ok(dataService.GetMediaTypes());

    [HttpGet("languages")]
    [ProducesResponseType<IEnumerable<Lang>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<Lang>> GetLanguages()
        => Ok(dataBaseService.GetLanguages());

    [HttpGet("content")]
    [ProducesResponseType<Page<ContentItem>>(StatusCodes.Status200OK)]
    public ActionResult<Page<ContentItem>> GetContentPaged(
        long page = 1,
        long pageSize = 50,
        string? name = null,
        string? alias = null,
        int? creatorId = null,
        string? id = null,
        int? level = null,
        bool? trashed = null,
        int? updaterId = null,
        int? languageId = null,
        int? missingLanguageId = null,
        int? publishedLanguageId = null,
        bool? edited = null,
        string orderBy = "N.id")
    {
        var criteria = new ContentCriteria
        {
            Name = name,
            Alias = alias,
            CreatorId = creatorId,
            Id = id,
            Level = level,
            Trashed = trashed,
            UpdaterId = updaterId,
            LanguageId = languageId,
            MissingLanguageId = missingLanguageId,
            PublishedLanguageId = publishedLanguageId,
            Edited = edited
        };

        return Ok(dataBaseService.GetContent(page, pageSize, criteria, orderBy));
    }

    [HttpGet("content/{id:int}/detail")]
    [ProducesResponseType<ContentMediaDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentMediaDetail>> GetContentDetail(int id)
    {
        var detail = await dataService.GetContentDetail(id);

        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpGet("content-type-aliases")]
    [ProducesResponseType<IEnumerable<string>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<string>> GetContentTypeAliases()
        => Ok(dataBaseService.GetContentTypeAliases());

    [HttpGet("standard-content-type-aliases")]
    [ProducesResponseType<IEnumerable<string>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<string>> GetStandardContentTypeAliases()
        => Ok(dataBaseService.GetContentTypeAliases(isElement: false));

    [HttpGet("members")]
    [ProducesResponseType<Page<MemberModel>>(StatusCodes.Status200OK)]
    public ActionResult<Page<MemberModel>> GetMembersPaged(
        long page = 1,
        long pageSize = 50,
        int? groupId = null,
        int? memberTypeId = null,
        bool? isApproved = null,
        bool? isLockedOut = null,
        bool? usesTwoFactor = null,
        string? search = null,
        string orderBy = "MN.text")
        => Ok(dataBaseService.GetMembers(page, pageSize, groupId, memberTypeId, isApproved, isLockedOut, usesTwoFactor, search, orderBy));

    [HttpGet("member-groups")]
    [ProducesResponseType<IEnumerable<MemberGroupModel>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<MemberGroupModel>> GetMemberGroups()
        => Ok(dataBaseService.GetMemberGroups());

    [HttpGet("member-types")]
    [ProducesResponseType<IEnumerable<MemberGroupModel>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<MemberGroupModel>> GetMemberTypes()
        => Ok(dataBaseService.GetMemberTypes());

    // ─── Reflection ──────────────────────────────────────────────────

    [HttpGet("reflection/surface-controllers")]
    [ProducesResponseType<IEnumerable<TypeMap>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TypeMap>> GetSurfaceControllers()
        => Ok(ReflectionHelper.GetTypeMapFrom(typeof(SurfaceController)));

    [HttpGet("reflection/api-controllers")]
    [ProducesResponseType<IEnumerable<TypeMap>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TypeMap>> GetApiControllers()
        => Ok(ReflectionHelper.GetTypeMapFrom(typeof(ControllerBase)));

    [HttpGet("reflection/render-controllers")]
    [ProducesResponseType<IEnumerable<TypeMap>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TypeMap>> GetRenderMvcControllers()
        => Ok(ReflectionHelper.GetTypeMapFrom(typeof(IRenderController)));

    [HttpGet("reflection/published-content-models")]
    [ProducesResponseType<IEnumerable<TypeMap>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TypeMap>> GetPublishedContentModels()
        => Ok(ReflectionHelper.GetPublishedContentModelTypeMap());

    [HttpGet("reflection/property-value-converters")]
    [ProducesResponseType<IEnumerable<TypeMap>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TypeMap>> GetPropertyValueConverters()
        => Ok(ReflectionHelper.GetTypeMapFrom(typeof(IPropertyValueConverter)));

    [HttpGet("reflection/composers")]
    [ProducesResponseType<IEnumerable<TypeMap>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TypeMap>> GetComposers()
        => Ok(ReflectionHelper.GetTypeMapFrom(typeof(IComposer)));

    [HttpGet("reflection/notification-handlers")]
    [ProducesResponseType<IEnumerable<TypeMap>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TypeMap>> GetNotificationHandlers()
        => Ok(ReflectionHelper.GetTypeMapFromOpenGeneric(typeof(INotificationHandler<>)));

    [HttpGet("reflection/hosted-services")]
    [ProducesResponseType<IEnumerable<TypeMap>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TypeMap>> GetHostedServices()
        => Ok(ReflectionHelper.GetTypeMapFrom(typeof(IHostedService)));

    [HttpGet("reflection/middleware")]
    [ProducesResponseType<IEnumerable<TypeMap>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TypeMap>> GetMiddleware()
        => Ok(ReflectionHelper.GetMiddlewareTypeMap());

    [HttpGet("reflection/view-components")]
    [ProducesResponseType<IEnumerable<TypeMap>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TypeMap>> GetViewComponents()
        => Ok(ReflectionHelper.GetTypeMapFrom(typeof(ViewComponent)));

    [HttpGet("reflection/content-finders")]
    [ProducesResponseType<IEnumerable<TypeMap>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TypeMap>> GetContentFinders()
        => Ok(ReflectionHelper.GetTypeMapFrom(typeof(IContentFinder)));

    [HttpGet("reflection/url-providers")]
    [ProducesResponseType<IEnumerable<TypeMap>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TypeMap>> GetUrlProviders()
        => Ok(ReflectionHelper.GetTypeMapFrom(typeof(IUrlProvider)));

    [HttpGet("reflection/tag-helpers")]
    [ProducesResponseType<IEnumerable<TypeMap>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TypeMap>> GetTagHelpers()
        => Ok(ReflectionHelper.GetTypeMapFrom(typeof(ITagHelper)).Where(x => !x.Name.StartsWith("__Generated")));

    [HttpGet("reflection/services")]
    [ProducesResponseType<IEnumerable<RegisteredService>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<RegisteredService>> GetRegisteredServices()
        => Ok(registeredServiceCollection.Services.Value);

    [HttpGet("reflection/types-assignable-from")]
    [ProducesResponseType<IEnumerable<TypeMap>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TypeMap>> GetTypesAssignableFrom([FromQuery] string baseType)
    {
        var t = Type.GetType(baseType);
        return Ok(t is null ? [] : ReflectionHelper.GetTypeMapFrom(t));
    }

    [HttpGet("reflection/type-detail")]
    [ProducesResponseType<TypeDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TypeDetail> GetTypeDetail([FromQuery] string loadableName)
    {
        var detail = ReflectionHelper.GetTypeDetail(loadableName);
        return detail is null ? NotFound() : Ok(detail);
    }

    // ─── Diagnostics ─────────────────────────────────────────────────

    [HttpGet("diagnostics")]
    [ProducesResponseType<IEnumerable<DiagnosticGroup>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<DiagnosticGroup>> GetEnvironmentDiagnostics()
    {
        diagnosticService.SetContext(HttpContext);
        return Ok(diagnosticService.GetDiagnosticGroups());
    }

    [HttpPost("diagnostics/reveal")]
    [ProducesResponseType<IEnumerable<DiagnosticGroup>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<DiagnosticGroup>> GetRevealedEnvironmentDiagnostics([FromBody] RevealDiagnosticsRequest? request)
    {
        if (!IsRevealPasswordValid(request?.Password))
        {
            return StatusCode(StatusCodes.Status403Forbidden, "The diagnostics reveal password is invalid or has not been configured.");
        }

        diagnosticService.SetContext(HttpContext);
        return Ok(diagnosticService.GetDiagnosticGroups(revealRedactedValues: true));
    }

    [HttpGet("key-values")]
    [ProducesResponseType<IEnumerable<UmbracoKeyValue>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<UmbracoKeyValue>> GetKeyValues()
        => Ok(dataBaseService.GetKeyValues());

    [HttpPut("key-values")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    public ActionResult<bool> UpdateKeyValue([FromQuery] string key, [FromBody] UpdateKeyValueRequest request)
        => Ok(dataBaseService.UpdateKeyValue(key, request.Value ?? string.Empty));

    [HttpPost("key-values")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    public ActionResult<bool> CreateKeyValue([FromBody] CreateKeyValueRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Key))
        {
            return BadRequest("Key is required.");
        }

        return Ok(dataBaseService.CreateKeyValue(request.Key.Trim(), request.Value ?? string.Empty));
    }

    [HttpDelete("key-values")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    public ActionResult<bool> DeleteKeyValue([FromQuery] string key)
        => Ok(dataBaseService.DeleteKeyValue(key));

    // ─── Assemblies ──────────────────────────────────────────────────

    [HttpGet("assemblies/umbraco")]
    [ProducesResponseType<IEnumerable<NameValue>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<NameValue>> GetUmbracoAssemblies()
        => Ok(ReflectionHelper.GetUmbracoAssemblies()
            .Select(a => new NameValue(a.GetName().Name, a.FullName))
            .OrderBy(x => x.Name));

    [HttpGet("assemblies/non-microsoft")]
    [ProducesResponseType<IEnumerable<NameValue>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<NameValue>> GetNonMsAssemblies()
        => Ok(ReflectionHelper.GetAssemblies()
            .Where(a => !a.IsDynamic && !a.FullName!.StartsWith("Microsoft.") && !a.FullName.StartsWith("System"))
            .Select(a => new NameValue(a.GetName().Name, a.FullName))
            .OrderBy(x => x.Name));

    [HttpGet("assemblies")]
    [ProducesResponseType<IEnumerable<NameValue>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<NameValue>> GetAssemblies()
        => Ok(ReflectionHelper.GetAssemblies(a => !a.IsDynamic)
            .Select(a => new NameValue(a.GetName().Name, a.FullName))
            .OrderBy(x => x.Name));

    [HttpGet("assemblies/with-interfaces")]
    [ProducesResponseType<IEnumerable<NameValue>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<NameValue>> GetAssembliesWithInterfaces()
        => Ok(ReflectionHelper.GetAssemblies(a => !a.IsDynamic && a.GetLoadableTypes().Any(t => t.IsInterface && !t.IsGenericTypeDefinition && t.IsPublic))
            .Select(a => new NameValue(a.GetName().Name, a.FullName))
            .OrderBy(x => x.Name));

    [HttpGet("assemblies/{assembly}/interfaces")]
    [ProducesResponseType<IEnumerable<TypeMap>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TypeMap>> GetInterfacesFrom(string assembly)
        => Ok(ReflectionHelper.GetInterfaceTypeMapWithImplementationCounts(Assembly.Load(assembly)).OrderBy(i => i.Name) ?? Enumerable.Empty<TypeMap>());

    [HttpGet("assemblies/{assembly}/types")]
    [ProducesResponseType<IEnumerable<TypeMap>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TypeMap>> GetTypesFrom(string assembly)
        => Ok(ReflectionHelper.GetNonGenericTypes(Assembly.Load(assembly)).OrderBy(i => i.Name) ?? Enumerable.Empty<TypeMap>());

    // ─── Templates / URLs ────────────────────────────────────────────

    [HttpGet("templates/urls-to-ping")]
    [ProducesResponseType<IEnumerable<string>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<string>> GetTemplateUrlsToPing()
        => Ok(dataBaseService.GetTemplateUrlsToPing());

    [HttpGet("urls-to-ping")]
    [ProducesResponseType<IEnumerable<string>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<string>> GetUrlsToPing([FromQuery] string culture = "")
        => Ok(utilitiesService.GetAllUrls(culture));

    [HttpGet("utilities/diagnostics")]
    [ProducesResponseType<UtilityDiagnostics>(StatusCodes.Status200OK)]
    public ActionResult<UtilityDiagnostics> GetUtilityDiagnostics()
        => Ok(utilitiesService.GetDiagnostics());

    [HttpGet("database/tables")]
    [ProducesResponseType<IEnumerable<DatabaseTableInfo>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<DatabaseTableInfo>> GetDatabaseTables()
        => Ok(dataBaseService.GetDatabaseTables());

    [HttpGet("database/tables/{tableName}")]
    [ProducesResponseType<DatabaseTableDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<DatabaseTableDetail> GetDatabaseTableDetail(string tableName)
    {
        var detail = dataBaseService.GetDatabaseTableDetail(tableName);

        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpGet("database/tables/{tableName}/rows")]
    [ProducesResponseType<DatabaseTableRows>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<DatabaseTableRows> GetDatabaseTableRows(string tableName, long page = 1, long pageSize = 25)
    {
        var rows = dataBaseService.GetDatabaseTableRows(tableName, page, pageSize);

        return rows is null ? NotFound() : Ok(rows);
    }

    [HttpGet("logs/overview")]
    [ProducesResponseType<GodModeLogOverview>(StatusCodes.Status200OK)]
    public ActionResult<GodModeLogOverview> GetLogOverview()
        => Ok(logService.GetOverview());

    [HttpGet("logs")]
    [ProducesResponseType<Page<GodModeLogEvent>>(StatusCodes.Status200OK)]
    public ActionResult<Page<GodModeLogEvent>> GetLogs(
        long page = 1,
        long pageSize = 25,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        string? level = null,
        string? search = null,
        string? queryExpression = null)
        => Ok(logService.GetLogs(page, pageSize, from, to, level, search, queryExpression));

    [HttpGet("logs/insights")]
    [ProducesResponseType<IEnumerable<GodModeLogInsight>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<GodModeLogInsight>> GetLogInsights(
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        int take = 8)
        => Ok(logService.GetInsights(from, to, take));

    [HttpGet("logs/level-counts")]
    [ProducesResponseType<IEnumerable<GodModeLogLevelCount>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<GodModeLogLevelCount>> GetLogLevelCounts(
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        string? search = null,
        string? queryExpression = null)
        => Ok(logService.GetLevelCounts(from, to, search, queryExpression));

    [HttpGet("logs/saved-queries")]
    [ProducesResponseType<IEnumerable<GodModeSavedLogQuery>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<GodModeSavedLogQuery>> GetSavedLogQueries()
        => Ok(logService.GetSavedQueries());

    // ─── Config / usage / tags ───────────────────────────────────────

    [HttpGet("config")]
    [ProducesResponseType<GodModeConfig>(StatusCodes.Status200OK)]
    public ActionResult<GodModeConfig> GetConfig()
        => Ok(godModeConfig.Value);

    [HttpGet("diagnostics/reveal-enabled")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    public ActionResult<bool> GetDiagnosticsRevealEnabled()
        => Ok(!string.IsNullOrEmpty(GetRevealPassword()));

    private bool IsRevealPasswordValid(string? password)
    {
        var expected = GetRevealPassword();

        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(password))
        {
            return false;
        }

        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var passwordBytes = Encoding.UTF8.GetBytes(password);

        return expectedBytes.Length == passwordBytes.Length &&
            CryptographicOperations.FixedTimeEquals(expectedBytes, passwordBytes);
    }

    private string? GetRevealPassword()
    {
        var environmentVariableName = godModeConfig.Value.Diagnostics.RedactRevealPasswordEnv;

        string password = string.IsNullOrWhiteSpace(environmentVariableName)
            ? null
            : Environment.GetEnvironmentVariable(environmentVariableName);

        return password;
    }

    [HttpGet("content-usage")]
    [ProducesResponseType<IEnumerable<UsageModel>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<UsageModel>> GetContentUsageData(int? id = null, string? orderBy = null)
        => Ok(dataBaseService.GetContentUsageData(id, orderBy));

    [HttpGet("tags")]
    [ProducesResponseType<IEnumerable<TagMapping>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TagMapping>>> GetTagMapping()
        => Ok(await dataService.GetTagMapping());

    [HttpGet("tags/orphaned")]
    [ProducesResponseType<List<Models.Tag>>(StatusCodes.Status200OK)]
    public ActionResult<List<Models.Tag>> GetOrphanedTags()
        => Ok(dataBaseService.GetOrphanedTags());

    [HttpDelete("tags/{id:int}")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    public ActionResult<bool> DeleteTag(int id)
        => Ok(dataBaseService.DeleteTag(id));

    // ─── NuCache ─────────────────────────────────────────────────────

    [HttpGet("nucache/{id:int}")]
    [ProducesResponseType<NuCacheItem>(StatusCodes.Status200OK)]
    public ActionResult<NuCacheItem> GetNuCacheItem(int id)
        => Ok(dataBaseService.GetNuCacheItem(id));

    [HttpGet("nucache/type")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public ActionResult<string> GetNuCacheType()
        => Ok(nuCacheSettings.NuCacheSerializerType.ToString());

    // ─── Maintenance actions (POST) ──────────────────────────────────

    [HttpPost("templates/fix-masters")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> FixTemplateMasters()
        => Ok(await dataService.FixTemplateMasters());

    [HttpPost("cache/clear")]
    [ProducesResponseType<ServerResponse>(StatusCodes.Status200OK)]
    public ActionResult<ServerResponse> ClearUmbracoCache([FromQuery] string cache)
        => Ok(utilitiesService.ClearUmbracoCacheFor(cache));

    [HttpPost("cache/purge-media")]
    [ProducesResponseType<ServerResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ServerResponse>> PurgeMediaCache()
        => Ok(await utilitiesService.ClearMediaFileCacheAsync());

    [HttpPost("app/restart")]
    [ProducesResponseType<ServerResponse>(StatusCodes.Status200OK)]
    public ActionResult<ServerResponse> RestartAppPool()
    {
        try
        {
            applicationLifetime?.StopApplication();
            return Ok(new ServerResponse("Restarting the application - hold tight...", ServerResponseType.Success));
        }
        catch (Exception ex)
        {
            return Ok(new ServerResponse("Error restarting the application: " + ex.Message, ServerResponseType.Error));
        }
    }

    [HttpPost("data-types/{id:int}/copy")]
    [ProducesResponseType<ServerResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ServerResponse>> CopyDataType(int id)
        => Ok(await dataService.CopyDataType(id));

    private async Task<IEnumerable<HealthRiskFinding>> BuildHealthRiskFindings()
    {
        const int logRowWarningThreshold = 100000;
        const int contentVersionWarningThreshold = 50;

        var findings = new List<HealthRiskFinding>();
        var contentTypes = dataService.GetContentTypeMap().ToList();
        var dataTypes = (await dataService.GetDataTypesStatus()).ToList();
        var templates = (await dataService.GetTemplates()).ToList();
        var referenceEdges = (await dataService.GetReferenceGraph()).ToList();
        var driftFindings = (await dataService.GetConfigurationDriftFindings()).ToList();
        var usage = dataBaseService.GetContentUsageData().ToList();
        var orphanedTags = dataBaseService.GetOrphanedTags();
        var orphanedMediaCount = dataBaseService.GetOrphanedMediaCount();
        var logRowCount = dataBaseService.GetLogRowCount();
        var contentVersionCount = dataBaseService.GetContentVersionCount();
        var contentWithExcessiveVersionsCount = dataBaseService.GetContentWithExcessiveVersionsCount(contentVersionWarningThreshold);
        var usageByAlias = usage
            .Where(x => !string.IsNullOrWhiteSpace(x.Alias))
            .GroupBy(x => x.Alias)
            .ToDictionary(x => x.Key, x => x.Sum(y => y.NodeCount));

        foreach (var edge in referenceEdges.Where(x => x.Relation == "configures missing block type"))
        {
            findings.Add(CreateFinding(
                "High",
                "Broken References",
                "Block editor references a missing element type",
                $"{edge.SourceName} references a block element type that was not found. Context: {edge.Context}.",
                "Data Type",
                edge.SourceName,
                edge.SourceAlias,
                edge.SourceKey,
                "Open the data type configuration and remove or replace the missing block type."));
        }

        foreach (var contentType in contentTypes.Where(x => !x.IsElement && !x.HasTemplates))
        {
            findings.Add(CreateFinding(
                "Low",
                "Content Model",
                "Document type has no allowed templates",
                $"{contentType.Name} is a document type, but it has no allowed templates. It may be intentional for headless/API-only content.",
                "Document Type",
                contentType.Name,
                contentType.Alias,
                contentType.Udi.ToString(),
                "Add an allowed template, mark it as an element type if it should never render, or keep it documented as API-only."));
        }

        foreach (var contentType in contentTypes.Where(x => !x.AllProperties.Any()))
        {
            findings.Add(CreateFinding(
                contentType.IsElement ? "Low" : "Medium",
                "Content Model",
                contentType.IsElement ? "Element type has no properties" : "Document type has no properties",
                $"{contentType.Name} has no own or inherited properties.",
                contentType.IsElement ? "Element Type" : "Document Type",
                contentType.Name,
                contentType.Alias,
                contentType.Udi.ToString(),
                "Confirm this is a deliberate structural type; otherwise add properties or remove the type."));
        }

        foreach (var contentType in contentTypes.Where(x => !x.IsElement && usageByAlias.TryGetValue(x.Alias, out var count) && count == 0))
        {
            findings.Add(CreateFinding(
                "Low",
                "Content Usage",
                "Document type has no content instances",
                $"{contentType.Name} exists in the schema but has no content items.",
                "Document Type",
                contentType.Name,
                contentType.Alias,
                contentType.Udi.ToString(),
                "Review whether this type is still needed or whether it is waiting for future content."));
        }

        foreach (var dataType in dataTypes.Where(x => !x.IsUsed && !x.IsNestedUsed))
        {
            findings.Add(CreateFinding(
                "Low",
                "Data Types",
                "Data type appears unused",
                $"{dataType.Name} is not used directly by document/media types and was not found in supported block editor configurations.",
                "Data Type",
                dataType.Name,
                dataType.Alias,
                dataType.Udi.ToString(),
                "Delete it if obsolete, or keep it if it is intentionally reserved for future schema work."));
        }

        foreach (var duplicateGroup in dataTypes.Where(x => !string.IsNullOrWhiteSpace(x.Name)).GroupBy(x => x.Name).Where(x => x.Count() > 1))
        {
            var aliases = string.Join(", ", duplicateGroup.Select(x => x.Alias).Distinct().OrderBy(x => x));
            findings.Add(CreateFinding(
                "Low",
                "Data Types",
                "Multiple data types share the same name",
                $"{duplicateGroup.Key} appears {duplicateGroup.Count()} times. Editor aliases: {aliases}.",
                "Data Type",
                duplicateGroup.Key,
                aliases,
                string.Empty,
                "Rename duplicates so future schema changes are easier to reason about."));
        }

        foreach (var drift in driftFindings.Where(x => x.Score >= 50).Take(25))
        {
            findings.Add(CreateFinding(
                drift.Severity,
                "Configuration Drift",
                drift.Category,
                drift.Summary,
                drift.EntityType,
                drift.EntityName,
                drift.EntityAlias,
                drift.EntityKey,
                drift.Recommendation));
        }

        var referencedTemplateKeys = referenceEdges
            .Where(x => x.TargetType == "Template")
            .Select(x => x.TargetKey)
            .ToHashSet();
        var templatesReferencedByTemplates = templates
            .SelectMany(template => template.Parents
                .Where(parent => parent.Id != template.Id)
                .Select(parent => parent.Udi.ToString()))
            .ToHashSet();

        foreach (var template in templates.Where(x => !referencedTemplateKeys.Contains(x.Udi.ToString()) && !templatesReferencedByTemplates.Contains(x.Udi.ToString())))
        {
            findings.Add(CreateFinding(
                "Low",
                "Templates",
                "Template is not allowed by any document type",
                $"{template.Name} exists but is not referenced by any document type allowed-template relationship or another template.",
                "Template",
                template.Name,
                template.Alias,
                template.Udi.ToString(),
                "Remove it if obsolete, or assign it to the document type that should render with it."));
        }

        if (orphanedTags.Any())
        {
            findings.Add(CreateFinding(
                "Low",
                "Content Cleanup",
                "Orphaned tags exist",
                $"{orphanedTags.Count} tags exist in the database but are not associated with any content or media.",
                "Tags",
                "Orphaned tags",
                string.Empty,
                string.Empty,
                "Use the Tag Browser to review and delete orphaned tags if they are no longer needed."));
        }

        if (orphanedMediaCount > 0)
        {
            findings.Add(CreateFinding(
                "Low",
                "Content Cleanup",
                "Orphaned media exists",
                $"{orphanedMediaCount} media items have no incoming Umbraco relation and may be unused.",
                "Media",
                "Orphaned media",
                string.Empty,
                string.Empty,
                "Review media usage before deleting; not every custom picker or rich text reference may create an Umbraco relation."));
        }

        if (logRowCount > logRowWarningThreshold)
        {
            findings.Add(CreateFinding(
                "Medium",
                "Database",
                "umbracoLog table is large",
                $"The log table contains {logRowCount:n0} rows, which can slow diagnostics and database maintenance.",
                "Database Table",
                "umbracoLog",
                string.Empty,
                string.Empty,
                "Review log retention, archive old rows, and investigate noisy recurring errors."));
        }

        if (contentWithExcessiveVersionsCount > 0)
        {
            findings.Add(CreateFinding(
                "Medium",
                "Database",
                "Content has many previous versions",
                $"{contentWithExcessiveVersionsCount} content items have more than {contentVersionWarningThreshold} versions. The version table contains {contentVersionCount:n0} rows in total.",
                "Database Table",
                "umbracoContentVersion",
                string.Empty,
                string.Empty,
                "Review content version cleanup settings and prune old versions where appropriate."));
        }

        return findings
            .Where(x => !IsIgnoredAlias(x.EntityAlias))
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Category)
            .ThenBy(x => x.Title)
            .ThenBy(x => x.EntityName);
    }

    private bool IsIgnoredAlias(string alias)
    {
        return !string.IsNullOrWhiteSpace(alias)
            && godModeConfig.Value.AliasesToIgnore.Any(x => alias.InvariantEquals(x));
    }

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

public sealed class RevealDiagnosticsRequest
{
    public string? Password { get; set; }
}
