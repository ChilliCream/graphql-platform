using System.Collections.Immutable;
using System.Text;

namespace Mocha;

public partial class MessageBusBuilder
{
    private static void ValidateDefaultTransports(ImmutableArray<MessagingTransport> transports)
    {
        var defaultTransportNames = transports
            .Where(t => t.IsDefaultTransport)
            .Select(t => t.Name)
            .ToArray();

        if (defaultTransportNames.Length > 1)
        {
            throw ThrowHelper.MultipleDefaultTransports(defaultTransportNames);
        }
    }

    private static void ValidateInboundRoutesAreBound(IMessagingSetupContext context)
    {
        var unboundRoutes = context.Router.InboundRoutes
            .Where(route =>
                route is { IsInitialized: true, IsCompleted: false, Endpoint: null, Kind: not InboundRouteKind.Reply })
            .OrderBy(route => route.Consumer?.Name, StringComparer.Ordinal)
            .ThenBy(route => route.MessageType?.RuntimeType.FullName, StringComparer.Ordinal)
            .ThenBy(route => route.Kind)
            .ToArray();

        if (unboundRoutes.Length == 0)
        {
            return;
        }

        throw new InvalidOperationException(context.FormatUnboundInboundRoutes(unboundRoutes));
    }
}

file static class Extensions
{
    public static string FormatUnboundInboundRoutes(
        this IMessagingSetupContext context,
        IReadOnlyList<InboundRoute> routes)
    {
        var sb = new StringBuilder();
        sb.Append("The message bus configuration has ");
        sb.Append(routes.Count);
        sb.Append(routes.Count == 1 ? " unbound inbound route." : " unbound inbound routes.");
        sb.AppendLine();
        sb.AppendLine("Each route below was registered but never attached to a receive endpoint.");

        for (var i = 0; i < routes.Count; i++)
        {
            var route = routes[i];
            sb.AppendLine();
            sb.Append('[');
            sb.Append(i + 1);
            sb.Append("] ");
            sb.Append(route.GetConsumerName());
            sb.Append(" (");
            sb.Append(route.GetConsumerType());
            sb.AppendLine(")");

            sb.Append("    message : ");
            sb.AppendLine(route.GetMessage());
            sb.Append("    kind    : ");
            sb.AppendLine(route.Kind.ToString());
            sb.Append("    where   : ");
            sb.AppendLine(route.GetCondition());
            sb.Append("    fix     : ");
            sb.AppendLine(route.GetHint(context));
        }

        return sb.ToString();
    }

    private static string GetHint(this InboundRoute route, IMessagingSetupContext context)
    {
        var sameMessageRoute = route.FindBoundRouteForSameMessage(context);
        if (sameMessageRoute is not null)
        {
            return "Another route for this message is already bound to consumer "
                + $"'{sameMessageRoute.GetConsumerName()}' on receive endpoint "
                + $"'{sameMessageRoute.Endpoint!.Name}' for transport "
                + $"'{sameMessageRoute.Endpoint.Transport.Name}'. "
                + "If this consumer is unintended, remove its registration. "
                + "If it should receive independently, bind it to its own receive endpoint.";
        }

        var transportHint = GetTransportHint(context);
        var endpointHint = GetEndpointHint(context);

        return transportHint
            + " If this consumer should receive over a transport, bind it to a receive endpoint. "
            + "If it is handled in-process only or superseded, remove it from message bus registration. "
            + endpointHint;
    }

    private static InboundRoute? FindBoundRouteForSameMessage(this InboundRoute route, IMessagingSetupContext context)
    {
        if (route.MessageType is not { } messageType)
        {
            return null;
        }

        return context.Router
            .GetInboundByMessageType(messageType)
            .Where(candidate =>
                !ReferenceEquals(candidate, route)
                && candidate.Kind == route.Kind
                && candidate.Endpoint is not null)
            .OrderBy(candidate => candidate.Consumer?.Name, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Endpoint?.Name, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private static string GetTransportHint(IMessagingSetupContext context)
    {
        if (context.Transports.Length == 0)
        {
            return "No messaging transports are configured, so no receive endpoint can be discovered.";
        }

        if (context.Transports.Length == 1)
        {
            var transport = context.Transports[0];
            return transport.BindMode == MessagingBindMode.Explicit
                ? $"Transport '{transport.Name}' uses explicit bind mode, so no convention receive endpoint is created for unbound consumers."
                : $"Transport '{transport.Name}' uses implicit bind mode, but it did not connect this route.";
        }

        var transports = string.Join(
            ", ",
            context.Transports.Select(static t => $"'{t.Name}' ({t.BindMode})"));

        return $"No configured transport connected this route. Configured transports: {transports}.";
    }

    private static string GetEndpointHint(IMessagingSetupContext context)
    {
        var endpoints = context.Transports
            .Select(static transport => new
            {
                Transport = transport.Name,
                Endpoints = transport.ReceiveEndpoints
                    .Where(static endpoint => endpoint.Kind == ReceiveEndpointKind.Default)
                    .Select(static endpoint => endpoint.Name)
                    .Order(StringComparer.Ordinal)
                    .ToArray()
            })
            .Where(static transport => transport.Endpoints.Length > 0)
            .OrderBy(static transport => transport.Transport, StringComparer.Ordinal)
            .ToArray();

        if (endpoints.Length == 0)
        {
            return "Available default receive endpoints: none.";
        }

        return "Available default receive endpoints: "
            + string.Join("; ", endpoints.Select(static t => $"{t.Transport}: {string.Join(", ", t.Endpoints)}"))
            + ".";
    }

    private static string GetConsumerName(this InboundRoute route)
        => route.Consumer?.Name ?? "(unknown consumer)";

    private static string GetConsumerType(this InboundRoute route)
        => route.Consumer?.Identity.FullName
            ?? route.Consumer?.Identity.Name
            ?? route.Consumer?.GetType().FullName
            ?? "(unknown consumer type)";

    private static string GetMessage(this InboundRoute route)
    {
        if (route.MessageType is null)
        {
            return "(no message type)";
        }

        return route.MessageType.Identity + " (" + route.MessageType.RuntimeType.FullName + ")";
    }

    private static string GetCondition(this InboundRoute route)
        => FormatCondition(route.Condition.Describe());

    private static string FormatCondition(RouteConditionDescription condition)
    {
        var text = condition.Detail is null
            ? condition.Kind
            : $"{condition.Kind}({condition.Detail})";

        return condition.Children.Count == 0
            ? text
            : text + "[" + string.Join(", ", condition.Children.Select(FormatCondition)) + "]";
    }
}
