using Mocha.Analyzers.Utils;

namespace Mocha.Analyzers;

/// <summary>
/// Represents the extracted metadata for a MessageBus handler discovered during source generation.
/// </summary>
/// <param name="HandlerTypeName">The fully qualified type name of the handler class.</param>
/// <param name="HandlerNamespace">The namespace containing the handler class.</param>
/// <param name="MessageTypeName">The fully qualified type name of the message the handler processes.</param>
/// <param name="ResponseTypeName">
/// The fully qualified type name of the response, or <see langword="null"/> if the handler returns no response.
/// </param>
/// <param name="Kind">The kind of messaging handler.</param>
/// <param name="MessageTypeHierarchy">
/// The unfiltered type hierarchy of the message type (base types excluding <c>object</c>, plus all interfaces),
/// as fully qualified display strings. Filtering to registered types happens in the generator phase.
/// </param>
/// <param name="Location">
/// The equatable source location of the handler type declaration, or <see langword="null"/> if unavailable.
/// </param>
public sealed record MessagingHandlerInfo(
    string HandlerTypeName,
    string HandlerNamespace,
    string MessageTypeName,
    string? ResponseTypeName,
    MessagingHandlerKind Kind,
    ImmutableEquatableArray<string> MessageTypeHierarchy,
    LocationInfo? Location) : SyntaxInfo
{
    /// <inheritdoc />
    public override string OrderByKey => $"MsgHandler:{Kind}:{MessageTypeName}:{HandlerTypeName}";
}
