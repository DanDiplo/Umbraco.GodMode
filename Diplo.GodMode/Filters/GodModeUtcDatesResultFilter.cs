using System.Text.Json;
using Diplo.GodMode.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;

namespace Diplo.GodMode.Filters;

/// <summary>
/// Makes GodMode API responses emit <see cref="DateTime"/> values as UTC (ISO 8601 with <c>Z</c>).
/// </summary>
/// <remarks>
/// Uses a copy of Umbraco's back office JSON options so the rest of the back office API is unaffected.
/// </remarks>
public sealed class GodModeUtcDatesResultFilter : IResultFilter
{
    private readonly SystemTextJsonOutputFormatter formatter;

    public GodModeUtcDatesResultFilter(IOptionsMonitor<JsonOptions> jsonOptions)
    {
        var serializerOptions = new JsonSerializerOptions(jsonOptions.Get(Constants.JsonOptionsNames.BackOffice).JsonSerializerOptions);
        serializerOptions.Converters.Insert(0, new UtcDateTimeJsonConverter());
        formatter = new SystemTextJsonOutputFormatter(serializerOptions);
    }

    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is ObjectResult result)
        {
            result.Formatters.Clear();
            result.Formatters.Add(formatter);
        }
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }
}
