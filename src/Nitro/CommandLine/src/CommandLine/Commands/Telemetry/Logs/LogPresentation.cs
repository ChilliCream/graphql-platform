using System.Globalization;
using ChilliCream.Nitro.Client.Telemetry.Models;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Logs;

internal static class LogPresentation
{
    public static string GetServiceName(IReadOnlyList<TelemetryAttribute> resourceAttributes)
        => resourceAttributes
            .FirstOrDefault(static attribute => attribute.Key == "service.name")
            ?.Value
            ?? string.Empty;

    public static string? GetAttributeValue(
        IReadOnlyList<TypedTelemetryAttribute> attributes,
        string key)
    {
        var attribute = attributes.FirstOrDefault(attribute => attribute.Key == key);
        return attribute is null ? null : FormatAttributeValue(attribute);
    }

    public static string FormatAttributeValue(TypedTelemetryAttribute attribute)
    {
        if (attribute.String is { } stringValue)
        {
            return stringValue;
        }

        if (attribute.Long is { } longValue)
        {
            return longValue.ToString(CultureInfo.InvariantCulture);
        }

        if (attribute.Float is { } floatValue)
        {
            return floatValue.ToString(CultureInfo.InvariantCulture);
        }

        return attribute.Boolean switch
        {
            true => "true",
            false => "false",
            _ => string.Empty
        };
    }
}
