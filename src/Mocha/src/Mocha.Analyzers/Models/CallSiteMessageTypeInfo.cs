namespace Mocha.Analyzers;

/// <summary>
/// Represents a message type discovered at a call site (method invocation) during source generation.
/// Call-site infos produce diagnostics only and do not generate code.
/// </summary>
/// <param name="MessageTypeName">The fully qualified type name of the discovered message type.</param>
/// <param name="Kind">The kind of call site where the message type was discovered.</param>
/// <param name="Location">
/// The equatable source location of the invocation expression, or <see langword="null"/> if unavailable.
/// </param>
/// <param name="ResponseTypeName">
/// The fully qualified type name of the response type for request-reply call sites, or <see langword="null"/>
/// for fire-and-forget calls.
/// </param>
/// <param name="DeclaredMessageType">
/// The declaration metadata (doc + span) of the statically resolved message type, captured cross-file from its
/// resolved symbol, or <see langword="null"/> when the message type has no source declaration in this
/// compilation. Consumed only by the message declaration pipeline, never by validation.
/// </param>
/// <param name="DeclaredResponseType">
/// The declaration metadata (doc + span) of the resolved response type, or <see langword="null"/> when the call
/// site resolves no response type or the response type has no source declaration in this compilation.
/// </param>
public sealed record CallSiteMessageTypeInfo(
    string MessageTypeName,
    CallSiteKind Kind,
    LocationInfo? Location,
    string? ResponseTypeName = null,
    DeclaredTypeInfo? DeclaredMessageType = null,
    DeclaredTypeInfo? DeclaredResponseType = null) : SyntaxInfo
{
    /// <inheritdoc />
    public override string OrderByKey => $"CallSite:{Kind}:{MessageTypeName}";
}
