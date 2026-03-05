using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Mocha.Transport.InMemory.Tests.Helpers;

public static partial class TopologySnapshotHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string CreateSnapshot(InMemoryMessagingTopology topology)
    {
        var topics = topology
            .Topics.Select(t => t.Name)
            .Where(n => !IsReplyName(n))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        var queues = topology
            .Queues.Select(q => q.Name)
            .Where(n => !IsReplyName(n))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        var bindings = topology
            .Bindings.Where(b => !IsReplyName(b.Source.Name))
            .Where(b =>
                b switch
                {
                    InMemoryQueueBinding qb => !IsReplyName(qb.Destination.Name),
                    InMemoryTopicBinding tb => !IsReplyName(tb.Destination.Name),
                    _ => true
                }
            )
            .Select(b =>
                b switch
                {
                    InMemoryQueueBinding qb => new BindingSnapshot(b.Source.Name, qb.Destination.Name, "Queue"),
                    InMemoryTopicBinding tb => new BindingSnapshot(b.Source.Name, tb.Destination.Name, "Topic"),
                    _ => new BindingSnapshot(b.Source.Name, "unknown", "Unknown")
                }
            )
            .OrderBy(b => b.Source, StringComparer.Ordinal)
            .ThenBy(b => b.Destination, StringComparer.Ordinal)
            .ThenBy(b => b.Kind, StringComparer.Ordinal)
            .ToList();

        var snapshot = new TopologySnapshot(topics, queues, bindings);
        return JsonSerializer.Serialize(snapshot, JsonOptions);
    }

    public static string CreateDescribeSnapshot(TransportDescription description)
    {
        var topology = description.Topology;

        var entities = (topology?.Entities ?? [])
            .Where(e => !IsReplyAddress(e.Address))
            .Select(e => new EntitySnapshot(e.Kind, e.Name, e.Flow))
            .OrderBy(e => e.Kind, StringComparer.Ordinal)
            .ThenBy(e => e.Name, StringComparer.Ordinal)
            .ToList();

        var links = (topology?.Links ?? [])
            .Where(l => !IsReplyAddress(l.Source) && !IsReplyAddress(l.Target))
            .Select(l => new LinkSnapshot(l.Kind, l.Direction))
            .OrderBy(l => l.Kind, StringComparer.Ordinal)
            .ThenBy(l => l.Direction, StringComparer.Ordinal)
            .ToList();

        var receiveEndpoints = description
            .ReceiveEndpoints.Where(e => e.Kind != ReceiveEndpointKind.Reply)
            .Select(e => new EndpointSnapshot(e.Name, e.Kind.ToString()))
            .OrderBy(e => e.Name, StringComparer.Ordinal)
            .ToList();

        var dispatchEndpoints = description
            .DispatchEndpoints.Where(e => e.Kind != DispatchEndpointKind.Reply)
            .Select(e => new EndpointSnapshot(e.Name, e.Kind.ToString()))
            .OrderBy(e => e.Name, StringComparer.Ordinal)
            .ToList();

        var snapshot = new DescribeSnapshot(
            description.Schema,
            description.TransportType,
            entities,
            links,
            receiveEndpoints,
            dispatchEndpoints);

        return JsonSerializer.Serialize(snapshot, JsonOptions);
    }

    private static bool IsReplyName(string name)
    {
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

    // Matches "response-{guid:N}" format (32 hex chars without hyphens)
    [GeneratedRegex(@"response-[0-9a-f]{32}", RegexOptions.IgnoreCase)]
    private static partial Regex ResponsePattern();

    private sealed record TopologySnapshot(List<string> Topics, List<string> Queues, List<BindingSnapshot> Bindings);

    private sealed record BindingSnapshot(string Source, string Destination, string Kind);

    private sealed record DescribeSnapshot(
        string Schema,
        string TransportType,
        List<EntitySnapshot> Entities,
        List<LinkSnapshot> Links,
        List<EndpointSnapshot> ReceiveEndpoints,
        List<EndpointSnapshot> DispatchEndpoints);

    private sealed record EntitySnapshot(string Kind, string? Name, string? Flow);

    private sealed record LinkSnapshot(string Kind, string? Direction);

    private sealed record EndpointSnapshot(string Name, string Kind);
}
