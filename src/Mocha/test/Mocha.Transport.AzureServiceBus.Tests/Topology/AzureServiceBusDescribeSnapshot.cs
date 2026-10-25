using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Mocha.Transport.AzureServiceBus.Tests.Topology;

/// <summary>
/// Projects a <see cref="TransportDescription"/> into a stable, deterministic JSON view for snapshot
/// assertions. Entities and links are sorted so that iteration order over the underlying topology
/// collections cannot flip the snapshot between otherwise-identical runs, and the transport's
/// instance-scoped reply queue (named with a fresh GUID on every host start) is filtered out because
/// its name is never stable between runs.
/// </summary>
internal static partial class AzureServiceBusDescribeSnapshot
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Create(TransportDescription description)
    {
        var topology = description.Topology;

        var entities = (topology?.Entities ?? [])
            .Where(e => !IsReplyName(e.Name))
            .Select(e => new EntitySnapshot(
                e.Kind,
                e.Name,
                e.Properties is not null && e.Properties.TryGetValue("autoProvision", out var ap) ? ap as bool? : null,
                e.Properties is not null && e.Properties.TryGetValue("origin", out var origin) ? origin as string : null))
            .OrderBy(e => e.Kind, StringComparer.Ordinal)
            .ThenBy(e => e.Name ?? string.Empty, StringComparer.Ordinal)
            .ToList();

        var links = (topology?.Links ?? [])
            .Where(l => !IsReplyName(l.Source) && !IsReplyName(l.Target))
            .Select(l => new LinkSnapshot(
                l.Kind,
                l.Source,
                l.Target,
                l.Properties is not null && l.Properties.TryGetValue("name", out var name) ? name as string : null,
                l.Properties is not null && l.Properties.TryGetValue("autoProvision", out var ap) ? ap as bool? : null,
                l.Properties is not null && l.Properties.TryGetValue("origin", out var origin) ? origin as string : null))
            .OrderBy(l => l.From ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(l => l.To ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(l => l.Name ?? string.Empty, StringComparer.Ordinal)
            .ToList();

        var snapshot = new DescribeSnapshot(description.Schema, description.TransportType, entities, links);
        return JsonSerializer.Serialize(snapshot, s_jsonOptions);
    }

    private static bool IsReplyName(string? value)
    {
        if (value is null)
        {
            return false;
        }

        return ResponsePattern().IsMatch(value);
    }

    // Matches "response-{guid:N}" format (32 hex chars without hyphens), the instance-scoped reply
    // queue name derived from a freshly generated host instance id on every transport start.
    [GeneratedRegex(@"response-[0-9a-f]{32}", RegexOptions.IgnoreCase)]
    private static partial Regex ResponsePattern();

    private sealed record DescribeSnapshot(
        string Schema,
        string TransportType,
        List<EntitySnapshot> Entities,
        List<LinkSnapshot> Links);

    private sealed record EntitySnapshot(string Kind, string? Name, bool? AutoProvision, string? Origin);

    private sealed record LinkSnapshot(
        string Kind,
        string? From,
        string? To,
        string? Name,
        bool? AutoProvision,
        string? Origin);
}
