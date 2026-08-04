using System.Buffers;
using System.Buffers.Binary;
using System.IO.Hashing;
using System.Text;

namespace Mocha;

/// <summary>
/// Builds the URN identity strings for message bus entities. URNs are stable identities shared
/// across services and tools, so the scheme is fixed rather than user-configurable.
/// </summary>
internal static class MochaUrn
{
    /// <summary>
    /// Returns a URN identifying the host process for the service.
    /// </summary>
    public static string Host(string service)
        => $"urn:mocha:svc:{service}:host";

    /// <summary>
    /// Returns the identity of a message type. Message identities are already canonical URNs.
    /// </summary>
    public static string MessageType(string identity)
        => identity;

    /// <summary>
    /// Returns a URN identifying a named consumer within the service.
    /// </summary>
    public static string Consumer(string service, string name)
        => $"urn:mocha:svc:{service}:consumer:{name}";

    /// <summary>
    /// Returns a URN identifying a named saga within the service.
    /// </summary>
    public static string Saga(string service, string name)
        => $"urn:mocha:svc:{service}:saga:{name}";

    /// <summary>
    /// Returns a URN identifying a specific state of a saga.
    /// </summary>
    public static string SagaState(string sagaUrn, string state)
        => $"{sagaUrn}:state:{state}";

    /// <summary>
    /// Returns a URN identifying a saga transition from a given state on a given event.
    /// </summary>
    public static string SagaTransition(string sagaUrn, string fromState, string eventName)
        => $"{sagaUrn}:transition:{fromState}:{eventName}";

    /// <summary>
    /// Returns a URN identifying a named transport within the service.
    /// </summary>
    public static string Transport(string service, string schema, string name)
        => $"urn:mocha:svc:{service}:transport:{schema}:{name}";

    /// <summary>
    /// Returns a URN identifying a named dispatch endpoint within a service transport.
    /// </summary>
    public static string DispatchEndpoint(string service, string schema, string transportName, string name)
        => $"urn:mocha:svc:{service}:transport:{schema}:{transportName}:dispatch-endpoint:{name}";

    /// <summary>
    /// Returns a URN identifying a named receive endpoint within a service transport.
    /// </summary>
    public static string ReceiveEndpoint(string service, string schema, string transportName, string name)
        => $"urn:mocha:svc:{service}:transport:{schema}:{transportName}:receive-endpoint:{name}";

    /// <summary>
    /// Returns a global URN identifying a topology entity. When <paramref name="address"/> is
    /// non-null, the address identifies the entity. Otherwise the kind and name are used.
    /// </summary>
    public static string TopologyEntity(string? address, string kind, string? name)
    {
        if (address is not null)
        {
            return $"urn:mocha:topology:{address}";
        }

        return $"urn:mocha:topology:{kind}:{name}";
    }

    /// <summary>
    /// Returns a global URN identifying a topology link between a source and target. When
    /// <paramref name="address"/> is non-null, the address identifies the link. Otherwise the kind,
    /// source, and target are used.
    /// </summary>
    public static string TopologyLink(string? address, string kind, string? source, string? target)
    {
        if (address is not null)
        {
            return $"urn:mocha:link:{address}";
        }

        return $"urn:mocha:link:{kind}:{source}~{target}";
    }

    /// <summary>
    /// Returns a URN identifying an inbound route by kind, consumer, and its match condition. The
    /// condition content disambiguates routes that share kind and consumer.
    /// </summary>
    public static string InboundRoute(string service, string kind, string? consumer, RouteConditionDescription condition)
        => $"urn:mocha:svc:{service}:inbound-route:{kind}:{consumer}:{HashCondition(condition):x16}";

    /// <summary>
    /// Returns a URN identifying an outbound route by kind, dispatch endpoint, and message type.
    /// </summary>
    public static string OutboundRoute(string service, string kind, string messageType, string dispatchEndpointName)
        => $"urn:mocha:svc:{service}:outbound-route:{kind}:{dispatchEndpointName}:{HashUtf8(messageType):x16}";

    private static ulong HashCondition(RouteConditionDescription condition)
    {
        var childCount = condition.Children.Count;

        if (childCount == 0)
        {
            return HashLeaf(condition.Kind, condition.Detail);
        }

        ulong[]? rented = null;
        var childHashes = childCount <= 16
            ? stackalloc ulong[16]
            : rented = ArrayPool<ulong>.Shared.Rent(childCount);

        try
        {
            var hashes = childHashes[..childCount];
            for (var i = 0; i < childCount; i++)
            {
                hashes[i] = HashCondition(condition.Children[i]);
            }

            // Composite conditions like And are commutative, so sort the child hashes to keep the
            // result order-independent and stable.
            hashes.Sort();

            return HashNode(condition.Kind, hashes);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<ulong>.Shared.Return(rented);
            }
        }
    }

    private static ulong HashLeaf(string kind, string? detail)
    {
        detail ??= "";

        var maxByteCount = Encoding.UTF8.GetMaxByteCount(kind.Length)
            + 1
            + Encoding.UTF8.GetMaxByteCount(detail.Length);
        byte[]? rented = null;
        var buffer = maxByteCount <= 256
            ? stackalloc byte[256]
            : rented = ArrayPool<byte>.Shared.Rent(maxByteCount);

        try
        {
            var written = Encoding.UTF8.GetBytes(kind, buffer);
            buffer[written++] = (byte)':';
            written += Encoding.UTF8.GetBytes(detail, buffer[written..]);
            return XxHash3.HashToUInt64(buffer[..written]);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    private static ulong HashUtf8(string value)
    {
        var maxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);
        byte[]? rented = null;
        var buffer = maxByteCount <= 256
            ? stackalloc byte[256]
            : rented = ArrayPool<byte>.Shared.Rent(maxByteCount);

        try
        {
            var written = Encoding.UTF8.GetBytes(value, buffer);
            return XxHash3.HashToUInt64(buffer[..written]);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    private static ulong HashNode(string kind, ReadOnlySpan<ulong> childHashes)
    {
        var maxByteCount = Encoding.UTF8.GetMaxByteCount(kind.Length) + childHashes.Length * sizeof(ulong);
        byte[]? rented = null;
        var buffer = maxByteCount <= 256
            ? stackalloc byte[256]
            : rented = ArrayPool<byte>.Shared.Rent(maxByteCount);

        try
        {
            var written = Encoding.UTF8.GetBytes(kind, buffer);
            foreach (var childHash in childHashes)
            {
                BinaryPrimitives.WriteUInt64LittleEndian(buffer[written..], childHash);
                written += sizeof(ulong);
            }

            return XxHash3.HashToUInt64(buffer[..written]);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }
}
