using System.Diagnostics.CodeAnalysis;

namespace Mocha.Transport.Nats;

/// <summary>
/// Builds and parses transport addresses of the form
/// <c>nats://host:port/&lt;stream&gt;/{s|c}/&lt;name&gt;</c>.
/// </summary>
// The s and c discriminators mirror the e and q segments the RabbitMQ transport uses, so reply
// addresses parse back the same way.
public static class NatsAddress
{
    /// <summary>
    /// The path segment identifying a subject.
    /// </summary>
    public const string SubjectSegment = "s";

    /// <summary>
    /// The path segment identifying a durable consumer.
    /// </summary>
    public const string ConsumerSegment = "c";

    /// <summary>
    /// Builds the address of a stream.
    /// </summary>
    /// <param name="baseAddress">The transport base address.</param>
    /// <param name="stream">The stream name.</param>
    /// <returns>The stream address.</returns>
    public static Uri ForStream(Uri baseAddress, string stream)
        => Combine(baseAddress, stream);

    /// <summary>
    /// Builds the address of a subject within a stream.
    /// </summary>
    /// <param name="baseAddress">The transport base address.</param>
    /// <param name="stream">The stream name.</param>
    /// <param name="subject">The subject.</param>
    /// <returns>The subject address.</returns>
    public static Uri ForSubject(Uri baseAddress, string stream, string subject)
        => Combine(baseAddress, stream, SubjectSegment, subject);

    /// <summary>
    /// Builds the address of a durable consumer within a stream.
    /// </summary>
    /// <param name="baseAddress">The transport base address.</param>
    /// <param name="stream">The stream name.</param>
    /// <param name="consumer">The durable consumer name.</param>
    /// <returns>The consumer address.</returns>
    public static Uri ForConsumer(Uri baseAddress, string stream, string consumer)
        => Combine(baseAddress, stream, ConsumerSegment, consumer);

    /// <summary>
    /// Attempts to parse an address into its stream, kind and name components.
    /// </summary>
    /// <param name="address">The address to parse.</param>
    /// <param name="stream">The stream name, when parsing succeeds.</param>
    /// <param name="kind">The discriminator, either <c>s</c> or <c>c</c>.</param>
    /// <param name="name">The subject or consumer name, when parsing succeeds.</param>
    /// <returns><see langword="true"/> when the address has the expected shape.</returns>
    public static bool TryParse(
        Uri? address,
        [NotNullWhen(true)] out string? stream,
        [NotNullWhen(true)] out string? kind,
        [NotNullWhen(true)] out string? name)
    {
        stream = null;
        kind = null;
        name = null;

        if (address is null)
        {
            return false;
        }

        var path = address.AbsolutePath.AsSpan();

        // One slot more than the three segments wanted: Split puts everything left over into the
        // final range instead of reporting an overflow, so a longer path would otherwise look like a
        // match with a trailing segment glued on.
        Span<Range> ranges = stackalloc Range[4];

        if (path.Split(ranges, '/', StringSplitOptions.RemoveEmptyEntries) != 3)
        {
            return false;
        }

        var kindSpan = path[ranges[1]];

        if (!kindSpan.SequenceEqual(SubjectSegment) && !kindSpan.SequenceEqual(ConsumerSegment))
        {
            return false;
        }

        stream = Uri.UnescapeDataString(path[ranges[0]].ToString());
        kind = kindSpan.SequenceEqual(SubjectSegment) ? SubjectSegment : ConsumerSegment;
        name = Uri.UnescapeDataString(path[ranges[2]].ToString());

        return true;
    }

    private static Uri Combine(Uri baseAddress, params string[] segments)
    {
        var path = string.Join('/', segments.Select(Uri.EscapeDataString));

        var builder = new UriBuilder(baseAddress)
        {
            Path = baseAddress.AbsolutePath.TrimEnd('/') + "/" + path
        };

        return builder.Uri;
    }
}
