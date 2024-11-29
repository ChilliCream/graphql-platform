namespace StrawberryShake;

/// <summary>
/// Specifies the GraphQL request strategy.
/// </summary>
public enum RequestStrategy
{
    /// <summary>
    /// The full GraphQL query is send.
    /// </summary>
    Default,

    /// <summary>
    /// An id is send representing the operation that is stored on the server.
    /// </summary>
    PersistedOperation,
}
