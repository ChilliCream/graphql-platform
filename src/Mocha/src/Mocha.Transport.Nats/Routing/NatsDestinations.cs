using System.Diagnostics.CodeAnalysis;
using static System.StringSplitOptions;

namespace Mocha.Transport.Nats;

/// <summary>
/// Resolves outbound routes to the subject they publish to.
/// </summary>
// NATS has a single destination kind, so this collapses the exchange and queue distinction the
// RabbitMQ transport has to make into one subject.
internal static class NatsDestinations
{
    /// <summary>
    /// Resolves the subject for an outbound route, honouring an explicit destination when present.
    /// </summary>
    /// <param name="schema">The transport schema.</param>
    /// <param name="naming">The bus naming conventions.</param>
    /// <param name="route">The outbound route.</param>
    /// <returns>The subject to publish to.</returns>
    public static string Resolve(string schema, IBusNamingConventions naming, OutboundRoute route)
    {
        if (route.HasExplicitDestination
            && route.Destination is { } destination
            && TryResolveExplicit(schema, destination, out var subject))
        {
            return subject;
        }

        return ResolveConvention(naming, route.Kind, route.MessageType);
    }

    /// <summary>
    /// Resolves the conventional subject for a message type. Send and Publish resolve to the same
    /// subject.
    /// </summary>
    /// <param name="naming">The bus naming conventions.</param>
    /// <param name="kind">The outbound route kind.</param>
    /// <param name="messageType">The message type.</param>
    /// <returns>The subject to publish to.</returns>
    // Converging on the publish name rather than the send name, because the send name is a single
    // bare token and would put every message type in one flat global namespace.
    public static string ResolveConvention(
        IBusNamingConventions naming,
        OutboundRouteKind kind,
        MessageType messageType)
        => kind switch
        {
            OutboundRouteKind.Send or OutboundRouteKind.Publish
                => naming.GetPublishEndpointName(messageType.RuntimeType),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

    /// <summary>
    /// Attempts to resolve an explicitly configured destination address to a subject.
    /// </summary>
    /// <param name="schema">The transport schema.</param>
    /// <param name="destination">The destination address.</param>
    /// <param name="subject">The resolved subject, when resolution succeeds.</param>
    /// <returns><see langword="true"/> when the address maps to a subject.</returns>
    public static bool TryResolveExplicit(
        string schema,
        Uri destination,
        [NotNullWhen(true)] out string? subject)
    {
        if (NatsAddress.TryParse(destination, out _, out var kind, out var name)
            && kind == NatsAddress.SubjectSegment)
        {
            subject = name;
            return true;
        }

        var path = destination.AbsolutePath.AsSpan();

        // One slot spare, so a path with more segments than expected is rejected rather than having
        // the remainder folded into the last range.
        Span<Range> ranges = stackalloc Range[3];
        var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

        if (destination.Scheme is "subject" && segmentCount == 1)
        {
            subject = Uri.UnescapeDataString(path[ranges[0]].ToString());
            return true;
        }

        if (destination.Scheme == schema
            && segmentCount == 2
            && path[ranges[0]].SequenceEqual(NatsAddress.SubjectSegment))
        {
            subject = Uri.UnescapeDataString(path[ranges[1]].ToString());
            return true;
        }

        subject = null;
        return false;
    }
}
