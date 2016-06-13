using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Diplo.GodMode.Models;
using Umbraco.Core;

namespace Diplo.GodMode.Helpers
{
    /// <summary>
    /// Helper for dealing with that nasty reflection stuff
    /// </summary>
    public static class ReflectionHelper
    {
        public static IEnumerable<Type> GetTypesAssignableFrom(Type baseType)
        {
            return AppDomain.CurrentDomain.GetAssemblies().
                SelectMany(s => GetTypesThatCanBeLoaded(s)).
                Where(p => baseType.IsAssignableFrom(p) && p.IsClass && !p.IsAbstract);
        }

        private static IEnumerable<Type> GetTypesThatCanBeLoaded(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }


        internal static IEnumerable<Diagnostic> PopulateDiagnosticsFrom(object obj)
        {
            if (obj == null)
            {
                return Enumerable.Empty<Diagnostic>();
            }

            var props = obj.GetType().GetAllProperties().Where(x => x.Module.Name.StartsWith("umbraco", StringComparison.OrdinalIgnoreCase));


            List<Diagnostic> diagnostics = new List<Diagnostic>(props.Count());

            foreach (var prop in props)
            {
                try
                {
                    diagnostics.Add(new Diagnostic(GetPropertyDisplayName(prop), prop.GetValue(obj)));
                }
                catch { };
            }

            return diagnostics;

        }

        private static string GetPropertyDisplayName(PropertyInfo prop)
        {
            return prop.Name.Split('.').Last().SplitPascalCasing() + (prop.PropertyType == typeof(bool) ? "?" : String.Empty);
        }
    }
}
