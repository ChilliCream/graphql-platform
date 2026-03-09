namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Describes the transport-level capabilities that a <see cref="ISourceSchemaClient"/>
/// supports. Because this is a <see cref="FlagsAttribute"/> enum, values can be combined
/// to advertise multiple capabilities at once.
/// </summary>
[Flags]
public enum SourceSchemaClientCapabilities
{
    /// <summary>
    /// The client supports variable batching, where a single request carries multiple
    /// sets of variables so the downstream service can resolve them in one round-trip.
    /// </summary>
    VariableBatching = 1,

    /// <summary>
    /// The client supports request batching, where multiple independent GraphQL
    /// operations are sent as an array in a single HTTP request.
    /// </summary>
    RequestBatching = 2,

    /// <summary>
    /// The client supports the Apollo-style request batching format, which uses a
    /// JSON array of operation objects as the request body.
    /// </summary>
    ApolloRequestBatching = 4,

    /// <summary>
    /// The client supports file upload via the
    /// <see href="https://github.com/jaydenseric/graphql-multipart-request-spec">
    /// GraphQL multipart request specification</see>.
    /// </summary>
    FileUpload = 8
}
