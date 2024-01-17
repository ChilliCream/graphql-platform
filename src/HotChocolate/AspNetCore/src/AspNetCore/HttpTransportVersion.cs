namespace HotChocolate.AspNetCore;

/// <summary>
/// Represents the GraphQL over HTTP transport version.
/// </summary>
public enum HttpTransportVersion
{
    /// <summary>
    /// Represents the latest released transport specification.
    /// </summary>
    Latest = 0,

    /// <summary>
    /// Represents the legacy specification version which will be cut off at 2025-01-01T00:00:00Z.
    /// </summary>
    Legacy = 1,

    /// <summary>
    /// Represents the GraphQL over HTTP spec version with the commit on 2023-01-27.
    /// </summary>
    Draft20230127 = 2,
}
