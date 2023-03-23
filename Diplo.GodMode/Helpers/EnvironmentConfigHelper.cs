using System;
using System.Collections.Generic;
using System.Linq;
using Diplo.GodMode.Models;
using Microsoft.Extensions.Configuration;

namespace Diplo.GodMode.Helpers
{
    internal class EnvironmentConfigHelper
    {
        /// <summary>
        /// Gets the environment variables from <see cref="IConfiguration"/>
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <returns>Diagnostics</returns>
        /// <remarks>
        /// This is very much indebted to some code from https://github.com/nul800sebastiaan/Cultiv.EnvironmentInspect/ #h5yr
        /// </remarks>
        public static List<DiagnosticSection> GetEnvironmentDiagnostics(IConfiguration configuration)
        {
            List<DiagnosticSection> sections = new();

            if (configuration is not IConfigurationRoot configurationRoot)
            {
                return sections;
            }

            int maxRecursions = 1000;

            Dictionary<string, List<Diagnostic>> diagnosticMap = new();

            void RecurseChildren(IEnumerable<IConfigurationSection> children)
            {
                maxRecursions--;

                foreach (var child in children.Where(x => !string.IsNullOrEmpty(x.Path)))
                {
                    var valueAndProvider = GetValueAndProvider(configurationRoot, child.Path);

                    if (valueAndProvider.Provider != null)
                    {
                        string provider = valueAndProvider.Provider.ToString();

                        if (diagnosticMap.TryGetValue(provider, out var diagnostics))
                        {
                            diagnostics.Add(new Diagnostic(child.Path, valueAndProvider.Value));
                        }
                        else
                        {
                            diagnosticMap.Add(provider, new List<Diagnostic>()
                            {
                                new Diagnostic(child.Path, valueAndProvider.Value)
                            });
                        }
                    }

                    if (maxRecursions > 0)
                    {
                        RecurseChildren(child.GetChildren());
                    }
                }
            }

            RecurseChildren(configurationRoot.GetChildren());

            foreach (var mapping in diagnosticMap.OrderBy(x => x.Key)) 
            { 
                sections.Add(new DiagnosticSection(mapping.Key, mapping.Value));
            }

            return sections;
        }

        private static (string Value, IConfigurationProvider Provider) GetValueAndProvider(IConfigurationRoot root, string key)
        {
            foreach (var provider in root.Providers.Reverse())
            {
                if (provider.TryGet(key, out var value))
                {
                    return (value, provider);
                }
            }

            return (null, null);
        }
    }
}
