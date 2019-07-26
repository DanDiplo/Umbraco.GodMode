using System.Collections.Generic;
using Diplo.GodMode.Models;

namespace Diplo.GodMode.Services.Interfaces
{
    /// <summary>
    /// Gets diagnostic information about Umbraco, server etc.
    /// </summary>
    public interface IDiagnosticService
    {
        IEnumerable<DiagnosticGroup> GetDiagnosticGroups();
    }
}