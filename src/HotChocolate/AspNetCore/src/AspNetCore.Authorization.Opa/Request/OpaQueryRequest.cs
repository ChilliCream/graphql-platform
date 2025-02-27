namespace HotChocolate.AspNetCore.Authorization;

/// <summary>
/// A structure representing OPA query request input.
/// </summary>
public sealed class OpaQueryRequest
{
    /// <summary>
    /// The constructor.
    /// </summary>
    /// <param name="policy">The instance of <see cref="Policy"/>.</param>
    /// <param name="request">The instance of <see cref="OriginalRequest"/>.
    ///     Stores information about the original GraphQl request.</param>
    /// <param name="source">The instance <see cref="IPAndPort"/>.
    ///     Stores information about source address of the original request</param>
    /// <param name="destination">The instance <see cref="IPAndPort"/>.
    ///     Stores information about destination address of the original request.</param>
    /// <param name="extensions">The instance of object that provides extended information for the OPA query request.
    ///     Usually represented as a dictionary.</param>
    public OpaQueryRequest(
        Policy policy,
        OriginalRequest request,
        IPAndPort source,
        IPAndPort destination,
        object? extensions = null)
    {
        ArgumentNullException.ThrowIfNull(policy);

        ArgumentNullException.ThrowIfNull(request);

        ArgumentNullException.ThrowIfNull(source);

        ArgumentNullException.ThrowIfNull(destination);

        Input = new OpaQueryRequestInput(policy, request, source, destination, extensions);
    }

    /// <summary>
    /// The property to get instance of <see cref="OpaQueryRequestInput"/>.
    /// </summary>
    public OpaQueryRequestInput Input { get; }

    /// <summary>
    /// The class representing an input that will be sent to the OPA server.
    /// </summary>
    /// <param name="policy">The instance of <see cref="Policy"/>.</param>
    /// <param name="request">The instance of <see cref="OriginalRequest"/>.
    ///     Stores information about the original GraphQl request.</param>
    /// <param name="source">The instance <see cref="IPAndPort"/>.
    ///     Stores information about source address of the original request</param>
    /// <param name="destination">The instance <see cref="IPAndPort"/>.
    ///     Stores information about destination address of the original request.</param>
    /// <param name="extensions">The instance of object the provides extended information for the OPA query request.
    ///     Usually represented as a dictionary.</param>
    public sealed class OpaQueryRequestInput(
        Policy policy,
        OriginalRequest request,
        IPAndPort source,
        IPAndPort destination,
        object? extensions)
    {
        /// <summary>
        /// The property to get instance of <see cref="Policy"/>.
        /// </summary>
        public Policy Policy { get; } = policy;

        /// <summary>
        /// The property to get instance of <see cref="OriginalRequest"/>.
        /// </summary>
        public OriginalRequest Request { get; } = request;

        /// <summary>
        /// The property to get instance of <see cref="IPAndPort"/> representing
        ///     the original request source IP address and port.
        /// </summary>
        public IPAndPort Source { get; } = source;

        /// <summary>
        /// The property to get instance of <see cref="IPAndPort"/> representing
        ///     the original request destination IP address and port.
        /// </summary>
        public IPAndPort Destination { get; } = destination;

        /// <summary>
        /// The property to get instance of object representing OPA query input extension data.
        /// </summary>
        public object? Extensions { get; } = extensions;
    }
}
