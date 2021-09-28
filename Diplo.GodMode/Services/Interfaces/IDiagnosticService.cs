using Diplo.GodMode.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Diplo.GodMode.Services.Interfaces
{
    /// <summary>
    /// Gets diagnostic information about Umbraco, server etc.
    /// </summary>
    public interface IDiagnosticService
    {
        IEnumerable<DiagnosticGroup> GetDiagnosticGroups();

        void SetContext(HttpContext httpContext);
    }
}