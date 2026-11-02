namespace Mocha.Analyzers;

/// <summary>
/// Defines the kinds of message handlers supported by the mediator.
/// </summary>
public enum HandlerKind
{
    /// <summary>
    /// A command handler that returns no response.
    /// </summary>
    Command,

    /// <summary>
    /// A command handler that returns a response.
    /// </summary>
    CommandResponse,

    /// <summary>
    /// A query handler that returns a single response.
    /// </summary>
    Query
}
