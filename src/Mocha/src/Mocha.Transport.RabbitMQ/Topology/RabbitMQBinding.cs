using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Base class for RabbitMQ bindings that route messages from a source exchange to a destination (queue or exchange).
/// </summary>
public abstract class RabbitMQBinding : TopologyResource<RabbitMQBindingConfiguration>, IRabbitMQResource
{
    /// <summary>
    /// Gets the source exchange from which messages are routed through this binding.
    /// </summary>
    public RabbitMQExchange Source { get; protected set; } = null!;

    /// <summary>
    /// Gets a value indicating whether this binding is automatically provisioned during topology setup.
    /// When <c>null</c>, the transport-level default is used.
    /// </summary>
    public bool? AutoProvision { get; protected set; }

    /// <summary>
    /// Gets the routing key pattern used to filter messages passing through this binding.
    /// </summary>
    public string RoutingKey { get; protected set; } = null!;

    /// <summary>
    /// Gets the additional binding arguments used for advanced routing (e.g., headers exchange matching).
    /// </summary>
    public ImmutableDictionary<string, object?> Arguments { get; protected set; } = ImmutableDictionary<string, object?>.Empty;

    internal void SetSource(RabbitMQExchange source)
    {
        Source = source;
    }

    /// <summary>
    /// Reconciles this binding with a duplicate declaration, keeping the stronger of the two values.
    /// An explicit auto-provision value wins over an inherited default, provisioning wins over a
    /// conflicting opt-out, and a declared origin wins over a framework-generated one.
    /// </summary>
    internal void MergeFrom(RabbitMQBindingConfiguration configuration)
    {
        if (AutoProvision is null)
        {
            AutoProvision = configuration.AutoProvision;
        }
        else if (configuration.AutoProvision == true)
        {
            AutoProvision = true;
        }

        MergeOrigin(configuration);
    }

    /// <summary>
    /// Determines whether two binding argument sets are equal for the purpose of binding identity.
    /// Equality is type sensitive: values of different runtime types are never equal, and binary
    /// values are compared by content. This matches how the broker treats header-table arguments.
    /// </summary>
    internal static bool ArgumentsEqual(
        IEnumerable<KeyValuePair<string, object?>>? left,
        IEnumerable<KeyValuePair<string, object?>>? right)
    {
        var leftMap = left?.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal) ?? [];
        var rightMap = right?.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal) ?? [];

        if (leftMap.Count != rightMap.Count)
        {
            return false;
        }

        foreach (var (key, leftValue) in leftMap)
        {
            if (!rightMap.TryGetValue(key, out var rightValue) || !ArgumentValueEquals(leftValue, rightValue))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ArgumentValueEquals(object? left, object? right)
    {
        if (left is null || right is null)
        {
            return left is null && right is null;
        }

        if (left.GetType() != right.GetType())
        {
            return false;
        }

        if (left is byte[] leftBytes && right is byte[] rightBytes)
        {
            return leftBytes.AsSpan().SequenceEqual(rightBytes);
        }

        return left.Equals(right);
    }

    /// <summary>
    /// Produces a stable, order-independent representation of binding arguments. The result is the
    /// same for the same set of key/value pairs regardless of insertion order or process, and it
    /// distinguishes values by type so that bindings differing only by their arguments resolve to
    /// distinct addresses.
    /// </summary>
    internal static string CanonicalizeArguments(IEnumerable<KeyValuePair<string, object?>>? arguments)
    {
        if (arguments is null)
        {
            return string.Empty;
        }

        var pairs = arguments
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => $"{Escape(kv.Key)}={FormatValue(kv.Value)}");

        return string.Join("&", pairs);
    }

    private static string Escape(string value)
        => value.Replace("%", "%25").Replace("=", "%3D").Replace("&", "%26");

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "null:",
            byte[] bytes => "bin:" + Convert.ToHexString(bytes).ToLowerInvariant(),
            _ => $"{value.GetType().Name}:{Escape(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty)}"
        };
    }

    /// <summary>
    /// Computes a short, deterministic discriminator for the given canonical argument string,
    /// suitable for distinguishing bind addresses that differ only by their arguments.
    /// </summary>
    internal static string ArgumentsDiscriminator(string canonicalArguments)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalArguments));
        return Convert.ToHexString(hash, 0, 4).ToLowerInvariant();
    }

    /// <summary>
    /// Builds the query component of a bind address, encoding the routing key and a stable arguments
    /// discriminator so that bindings differing only by routing key or arguments resolve to distinct
    /// addresses.
    /// </summary>
    private protected static string BuildQuery(
        string routingKey,
        ImmutableDictionary<string, object?> arguments)
    {
        var parts = new List<string>(2);

        if (!string.IsNullOrEmpty(routingKey))
        {
            parts.Add("rk=" + Uri.EscapeDataString(routingKey));
        }

        if (!arguments.IsEmpty)
        {
            parts.Add("args=" + ArgumentsDiscriminator(CanonicalizeArguments(arguments)));
        }

        return string.Join("&", parts);
    }

    /// <summary>
    /// Declares this binding on the broker using the specified channel.
    /// </summary>
    /// <param name="channel">The RabbitMQ channel to use for declaring the binding.</param>
    /// <param name="cancellationToken">A token to cancel the provisioning operation.</param>
    public abstract Task ProvisionAsync(IChannel channel, CancellationToken cancellationToken);
}

/// <summary>
/// Represents a binding that routes messages from a source exchange to a destination exchange.
/// </summary>
public sealed class RabbitMQExchangeBinding : RabbitMQBinding
{
    /// <summary>
    /// Gets the destination exchange that receives messages routed through this binding.
    /// </summary>
    public RabbitMQExchange Destination { get; private set; } = null!;

    protected override void OnInitialize(RabbitMQBindingConfiguration configuration)
    {
        RoutingKey = configuration.RoutingKey ?? string.Empty;
        Arguments = configuration.Arguments?.ToImmutableDictionary(kv => kv.Key, kv => (object?)kv.Value) ?? ImmutableDictionary<string, object?>.Empty;
        AutoProvision = configuration.AutoProvision;
    }

    protected override void OnComplete(RabbitMQBindingConfiguration configuration)
    {
        var builder = new UriBuilder(Topology.Address);
        builder.Path = Topology.Address.AbsolutePath.TrimEnd('/') + "/b/e/" + Source.Name + "/e/" + Destination.Name;
        builder.Query = BuildQuery(RoutingKey, Arguments);
        Address = builder.Uri;
    }

    internal void SetDestination(RabbitMQExchange destination)
    {
        Destination = destination;
    }

    /// <inheritdoc />
    public override async Task ProvisionAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeBindAsync(
            Destination.Name,
            Source.Name,
            RoutingKey,
            Arguments,
            cancellationToken: cancellationToken);
    }
}

/// <summary>
/// Represents a binding that routes messages from a source exchange to a destination queue.
/// </summary>
public sealed class RabbitMQQueueBinding : RabbitMQBinding
{
    /// <summary>
    /// Gets the destination queue that receives messages routed through this binding.
    /// </summary>
    public RabbitMQQueue Destination { get; private set; } = null!;

    protected override void OnInitialize(RabbitMQBindingConfiguration configuration)
    {
        RoutingKey = configuration.RoutingKey ?? string.Empty;
        Arguments = configuration.Arguments?.ToImmutableDictionary(kv => kv.Key, kv => (object?)kv.Value) ?? ImmutableDictionary<string, object?>.Empty;
        AutoProvision = configuration.AutoProvision;
    }

    protected override void OnComplete(RabbitMQBindingConfiguration configuration)
    {
        var builder = new UriBuilder(Topology.Address);
        builder.Path = Topology.Address.AbsolutePath.TrimEnd('/') + "/b/e/" + Source.Name + "/q/" + Destination.Name;
        builder.Query = BuildQuery(RoutingKey, Arguments);
        Address = builder.Uri;
    }

    internal void SetDestination(RabbitMQQueue destination)
    {
        Destination = destination;
    }

    /// <inheritdoc />
    public override async Task ProvisionAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.QueueBindAsync(
            Destination.Name,
            Source.Name,
            RoutingKey,
            Arguments,
            cancellationToken: cancellationToken);
    }
}
