namespace HotChocolate.AspNetCore;

/// <summary>
/// Represents the GraphQL over HTTP transport version.
/// </summary>
public enum HttpTransportVersion
{
    /// <summary>
    /// Resolves at runtime to the transport specification revision the server uses by default,
    /// currently <see cref="Draft20250508"/>.
    /// </summary>
    Latest = 0,

    /// <summary>
    /// Represents the legacy specification version which will be cut off at 2025-01-01T00:00:00Z.
    /// </summary>
    Legacy = 1,

    /// <summary>
    /// Represents the GraphQL over HTTP spec version with the commit on 2023-01-27.
    /// The server implements no behavior specific to this revision and resolves it at runtime
    /// to <see cref="Draft20250508"/>.
    /// </summary>
    Draft20230127 = 2,

    /// <summary>
    /// Represents the GraphQL over HTTP spec version with the commit on 2025-05-08.
    /// </summary>
    Draft20250508 = 3
}
