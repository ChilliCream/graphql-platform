namespace Mocha.Analyzers;

/// <summary>
/// Represents the extracted metadata for a command or query handler discovered during source generation.
/// </summary>
/// <param name="HandlerTypeName">The simple type name of the handler class.</param>
/// <param name="HandlerNamespace">The namespace containing the handler class.</param>
/// <param name="MessageTypeName">The simple type name of the message the handler processes.</param>
/// <param name="ResponseTypeName">
/// The simple type name of the response, or <see langword="null"/> if the handler returns no response.
/// </param>
/// <param name="Kind">The kind of handler.</param>
public sealed record HandlerInfo(
    string HandlerTypeName,
    string HandlerNamespace,
    string MessageTypeName,
    string? ResponseTypeName,
    HandlerKind Kind) : SyntaxInfo
{
    /// <inheritdoc />
    public override string OrderByKey => $"{Kind}:{MessageTypeName}";
}
