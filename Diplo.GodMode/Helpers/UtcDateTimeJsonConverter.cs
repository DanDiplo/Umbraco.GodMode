using System.Text.Json;
using System.Text.Json.Serialization;

namespace Diplo.GodMode.Helpers;

/// <summary>
/// Serialises <see cref="DateTime"/> values as UTC ISO 8601 strings (with a trailing <c>Z</c>).
/// </summary>
/// <remarks>
/// Umbraco 17 stores system dates as UTC, but values read through raw SQL come back with
/// <see cref="DateTimeKind.Unspecified"/> and would otherwise be serialised without an offset,
/// causing the browser to treat them as local time. Unspecified values are therefore assumed
/// to be UTC, and local values are converted to UTC.
/// </remarks>
public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => ToUtc(reader.GetDateTime());

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(ToUtc(value));

    public static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
