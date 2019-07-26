using Umbraco.Core;
using Umbraco.Core.Composing;
using Diplo.GodMode.Services;
using Diplo.GodMode.Services.Interfaces;

namespace Diplo.GodMode.Composers
{
    /// <summary>
    /// Used to register the God Mode services with DI
    /// </summary>
    /// <remarks>
    /// See https://our.umbraco.com/Documentation/Reference/using-ioc
    /// </remarks>
    public class LogComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            composition.Register<IDiagnosticService, DiagnosticService>();
            composition.Register<IUmbracoDatabaseService, UmbracoDatabaseService>();
            composition.Register<IUmbracoDataService, UmbracoDataService>();
        }
    }
}
