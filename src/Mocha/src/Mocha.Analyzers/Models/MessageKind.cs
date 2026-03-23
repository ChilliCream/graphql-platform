namespace Mocha.Analyzers;

/// <summary>
/// Defines the kinds of message types discovered during source generation.
/// </summary>
public enum MessageKind
{
    /// <summary>
    /// A void command (no response).
    /// </summary>
    CommandVoid,

    /// <summary>
    /// A command that returns a response.
    /// </summary>
    CommandResponse,

    /// <summary>
    /// A query that returns a response.
    /// </summary>
    Query
}
