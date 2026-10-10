using System.Diagnostics;

namespace Diplo.GodMode.Helpers;

internal static class ProcessTimeHelper
{
    internal static DateTime GetStartedUtc(DateTime fallbackUtc)
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            return GetStartedUtc(process, fallbackUtc);
        }
        catch
        {
            return fallbackUtc;
        }
    }

    internal static DateTime GetStartedUtc(Process process, DateTime fallbackUtc)
    {
        try
        {
            return process.StartTime.ToUniversalTime();
        }
        catch
        {
            // Process metadata can be unavailable in restricted hosting environments.
            return fallbackUtc;
        }
    }
}
