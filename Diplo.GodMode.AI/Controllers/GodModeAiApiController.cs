using Asp.Versioning;
using Diplo.GodMode.AI.Services;
using Diplo.GodMode.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Cms.Web.Common.Authorization;

namespace Diplo.GodMode.AI.Controllers
{
    [ApiVersion("1.0")]
    [VersionedApiBackOfficeRoute("godmode-ai")]
    [ApiExplorerSettings(GroupName = "GodModeAI")]
    [Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
    public class GodModeAiApiController : ManagementApiControllerBase
    {
        private readonly IGodModeAiAnalysisService analysisService;
        private readonly IGodModeAiExplainService explainService;

        public GodModeAiApiController(
            IGodModeAiAnalysisService analysisService,
            IGodModeAiExplainService explainService)
        {
            this.analysisService = analysisService;
            this.explainService = explainService;
        }

        [HttpPost("schema-health")]
        [ProducesResponseType<GodModeAnalysisResult>(StatusCodes.Status200OK)]
        public async Task<ActionResult<GodModeAnalysisResult>> AnalyzeSchemaHealth(CancellationToken cancellationToken)
            => Ok(await analysisService.AnalyzeSchemaHealthAsync(cancellationToken));

        [HttpPost("fix-plan")]
        [ProducesResponseType<GodModeAnalysisResult>(StatusCodes.Status200OK)]
        public async Task<ActionResult<GodModeAnalysisResult>> CreateFixPlan(CancellationToken cancellationToken)
            => Ok(await analysisService.CreateFixPlanAsync(cancellationToken));

        [HttpPost("explain")]
        [ProducesResponseType<GodModeAiExplainResponse>(StatusCodes.Status200OK)]
        public async Task<ActionResult<GodModeAiExplainResponse>> Explain(
            GodModeAiExplainRequest request,
            CancellationToken cancellationToken)
            => Ok(await explainService.ExplainAsync(request, cancellationToken));
    }
}
