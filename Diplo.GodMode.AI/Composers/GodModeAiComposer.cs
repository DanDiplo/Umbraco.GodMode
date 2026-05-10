using Diplo.GodMode.AI.Services;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Diplo.GodMode.AI.Composers
{
    public class GodModeAiComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.Configure<GodModeAiConfig>(builder.Config.GetSection(GodModeAiConfig.ConfigSectionName));
            builder.Services.AddScoped<IGodModeAiExplainService, GodModeAiExplainService>();
        }
    }
}
