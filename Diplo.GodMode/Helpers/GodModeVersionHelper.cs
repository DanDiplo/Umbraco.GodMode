using System.Reflection;

namespace Diplo.GodMode.Helpers;

internal static class GodModeVersionHelper
{
    public static string GetVersion()
    {
        var assembly = typeof(GodModeVersionHelper).Assembly;
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? string.Empty;
    }
}
