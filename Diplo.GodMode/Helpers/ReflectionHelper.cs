using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Diplo.GodMode.Helpers
{
    /// <summary>
    /// Helper for dealing with that nasty reflection stuff
    /// </summary>
    internal static class ReflectionHelper
    {
        internal static IEnumerable<Type> GetTypesAssignableFrom(Type baseType)
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
    }
}
