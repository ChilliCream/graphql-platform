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
public sealed record CallSiteMessageTypeInfo(
    string MessageTypeName,
    CallSiteKind Kind,
    LocationInfo? Location,
    string? ResponseTypeName = null) : SyntaxInfo
{
    /// <inheritdoc />
    public override string OrderByKey => $"CallSite:{Kind}:{MessageTypeName}";
}
