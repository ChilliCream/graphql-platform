namespace Mocha.Mediator;

/// <summary>
/// Specifies the kind of mediator handler.
/// </summary>
public enum MediatorHandlerKind
{
    /// <summary>
    /// A command handler that does not return a response.
    /// </summary>
    Command,

    /// <summary>
    /// A command handler that returns a response.
    /// </summary>
    CommandResponse,

    /// <summary>
    /// A query handler that returns a response.
    /// </summary>
    Query,

    /// <summary>
    /// A notification handler. Multiple handlers can be registered for the same notification type.
    /// </summary>
    Notification
}
