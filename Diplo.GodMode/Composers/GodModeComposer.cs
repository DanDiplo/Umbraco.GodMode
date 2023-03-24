using Diplo.GodMode.Models;
using Diplo.GodMode.Services;
using Diplo.GodMode.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Diplo.GodMode.Composers
{
    /// <summary>
    /// Used to register the God Mode services with DI
    /// </summary>
    public class GodModeComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddScoped<IDiagnosticService, DiagnosticService>();
            builder.Services.AddScoped<IUmbracoDatabaseService, UmbracoDatabaseService>();
            builder.Services.AddScoped<IUmbracoDataService, UmbracoDataService>();
            builder.Services.AddScoped<IUtilitiesService, UtilitiesService>();

            RegisteredServiceCollection registeredServiceCollection = new RegisteredServiceCollection(builder.Services);
            builder.Services.AddSingleton<RegisteredServiceCollection>(services => registeredServiceCollection);
        }
    }
}
