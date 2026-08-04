using Mocha.Analyzers.Utils;

namespace Mocha.Analyzers;

/// <summary>
/// Represents the extracted metadata for a MessageBus handler discovered during source generation.
/// </summary>
/// <param name="HandlerTypeName">The fully qualified type name of the handler class.</param>
/// <param name="HandlerNamespace">The namespace containing the handler class.</param>
/// <param name="MessageTypeName">The fully qualified type name of the message the handler processes.</param>
/// <param name="MessageNamespace">The namespace containing the message type.</param>
/// <param name="ResponseTypeName">
/// The fully qualified type name of the response, or <see langword="null"/> if the handler returns no response.
/// </param>
/// <param name="ResponseNamespace">
/// The namespace containing the response type, or <see langword="null"/> if the handler returns no response.
/// </param>
/// <param name="Kind">The kind of messaging handler.</param>
/// <param name="MessageTypeHierarchy">
/// The unfiltered type hierarchy of the message type (base types excluding <c>object</c>, plus all interfaces),
/// as fully qualified display strings. Filtering to registered types happens in the generator phase.
/// </param>
/// <param name="XmlDocumentation">The XML documentation captured from the handler declaration.</param>
/// <param name="Location">
/// The equatable source location used for diagnostics, or <see langword="null"/> if unavailable.
/// </param>
/// <param name="DeclarationLocation">
/// The equatable source location of the handler type declaration, or <see langword="null"/> if unavailable.
/// </param>
/// <param name="DeclaredMessageType">
/// The declaration metadata (doc + span) of the message type, captured cross-file from its resolved symbol,
/// or <see langword="null"/> when the message type has no source declaration in this compilation. This is
/// one of several discovery sources merged by fully qualified name in the message declaration pipeline.
/// </param>
/// <param name="DeclaredResponseType">
/// The declaration metadata (doc + span) of the response type, captured cross-file from its resolved symbol,
/// or <see langword="null"/> when the handler returns no response or the response type has no source
/// declaration in this compilation.
/// </param>
public sealed record MessagingHandlerInfo(
    string HandlerTypeName,
    string HandlerNamespace,
    string MessageTypeName,
    string MessageNamespace,
    string? ResponseTypeName,
    string? ResponseNamespace,
    MessagingHandlerKind Kind,
    ImmutableEquatableArray<string> MessageTypeHierarchy,
    string? XmlDocumentation,
    LocationInfo? Location,
    LocationInfo? DeclarationLocation,
    DeclaredTypeInfo? DeclaredMessageType,
    DeclaredTypeInfo? DeclaredResponseType) : SyntaxInfo
{
    /// <inheritdoc />
    public override string OrderByKey => $"MsgHandler:{Kind}:{MessageTypeName}:{HandlerTypeName}";
}
