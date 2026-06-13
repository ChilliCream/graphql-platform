using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Mocha.Transport.RabbitMQ.Tests.Helpers;

/// <summary>
/// Projects a <see cref="TransportDescription"/> into a stable, deterministic JSON view for snapshot
/// assertions. Reply and response plumbing (instance queues named with GUIDs or "response-{guid}")
/// is filtered out because those names are non-deterministic between runs.
/// </summary>
internal static partial class RabbitMQDescribeSnapshot
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
            .Where(e => !IsReplyName(e.Name) && !IsReplyAddress(e.Address))
            .Select(e => new EntitySnapshot(
                e.Kind,
                e.Name,
                e.Properties is not null && e.Properties.TryGetValue("autoProvision", out var ap) ? ap as bool? : null,
                e.Properties is not null && e.Properties.TryGetValue("source", out var src) ? src as string : null))
            .OrderBy(e => e.Kind, StringComparer.Ordinal)
            .ThenBy(e => e.Name ?? string.Empty, StringComparer.Ordinal)
            .ToList();

        var links = (topology?.Links ?? [])
            .Where(l =>
                !IsReplyAddress(l.Source)
                && !IsReplyAddress(l.Target)
                && !IsReplyAddress(l.Address))
            .Select(l => new LinkSnapshot(
                l.Kind,
                l.Source,
                l.Target,
                l.Properties is not null && l.Properties.TryGetValue("routingKey", out var rk) ? rk as string : null,
                l.Properties is not null && l.Properties.TryGetValue("autoProvision", out var ap) ? ap as bool? : null,
                l.Properties is not null && l.Properties.TryGetValue("source", out var src) ? src as string : null))
            .OrderBy(l => l.From ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(l => l.To ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(l => l.RoutingKey ?? string.Empty, StringComparer.Ordinal)
            .ToList();

        var snapshot = new DescribeSnapshot(description.Schema, description.TransportType, entities, links);
        return JsonSerializer.Serialize(snapshot, s_jsonOptions);
    }

    private static bool IsReplyName(string? name)
    {
        if (name is null)
        {
            return false;
        }

        return GuidPattern().IsMatch(name) || ResponsePattern().IsMatch(name);
    }

    private static bool IsReplyAddress(string? address)
    {
        if (address is null)
        {
            return false;
        }

        return GuidPattern().IsMatch(address) || ResponsePattern().IsMatch(address);
    }

    [GeneratedRegex(@"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}", RegexOptions.IgnoreCase)]
    private static partial Regex GuidPattern();

    // Matches "response-{guid:N}" format (32 hex chars without hyphens).
    [GeneratedRegex(@"response-[0-9a-f]{32}", RegexOptions.IgnoreCase)]
    private static partial Regex ResponsePattern();

    private sealed record DescribeSnapshot(
        string Schema,
        string TransportType,
        List<EntitySnapshot> Entities,
        List<LinkSnapshot> Links);

    private sealed record EntitySnapshot(string Kind, string? Name, bool? AutoProvision, string? Source);

    private sealed record LinkSnapshot(
        string Kind,
        string? From,
        string? To,
        string? RoutingKey,
        bool? AutoProvision,
        string? Source);
}
