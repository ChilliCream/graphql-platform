namespace Mocha.Mediator;

/// <summary>
/// Describes a single registered mediator handler for diagnostic and visualization purposes.
/// </summary>
/// <param name="Id">The stable URN identity of the handler.</param>
/// <param name="Name">The readable handler type name.</param>
/// <param name="Kind">The kind of handler (command, command with response, query, or notification).</param>
/// <param name="MessageId">The URN identity of the message the handler handles.</param>
/// <param name="MessageName">The readable message type name.</param>
/// <param name="ResponseTypeName">The readable response type name, or <c>null</c> for void commands and notifications.</param>
/// <param name="Source">Source metadata captured from the handler declaration, or <c>null</c> when not provided.</param>
public sealed record MediatorHandlerDescription(
    string Id,
    string Name,
    MediatorHandlerKind Kind,
    string MessageId,
    string MessageName,
    string? ResponseTypeName,
    SourceMetadata? Source);
