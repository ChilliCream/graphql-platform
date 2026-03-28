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
/// <param name="Location">
/// The equatable source location of the handler type declaration, or <see langword="null"/> if unavailable.
/// </param>
public sealed record MessagingHandlerInfo(
    string HandlerTypeName,
    string HandlerNamespace,
    string MessageTypeName,
    string? ResponseTypeName,
    MessagingHandlerKind Kind,
    LocationInfo? Location) : SyntaxInfo
{
    /// <inheritdoc />
    public override string OrderByKey => $"MsgHandler:{Kind}:{MessageTypeName}:{HandlerTypeName}";
}
