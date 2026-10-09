namespace Mocha.Analyzers;

/// <summary>
/// Represents a message type discovered from an explicit <c>AddMessage&lt;TMessage&gt;()</c> registration on
/// the Mocha builder APIs. This is a metadata-only discovery source: it carries declaration metadata into the
/// message declaration pipeline and is deliberately ignored by the AOT JsonSerializerContext validation
/// (MO0015/MO0016/MO0018) and by code emission, which is why it is a distinct record from
/// <see cref="CallSiteMessageTypeInfo"/>.
/// </summary>
/// <param name="DeclaredMessageType">
/// The declaration metadata (doc + span) of the registered message type, captured cross-file from its resolved
/// symbol. Only types with a source declaration in this compilation produce an entry.
/// </param>
public sealed record AddMessageDeclarationInfo(
    DeclaredTypeInfo DeclaredMessageType) : SyntaxInfo
{
    /// <inheritdoc />
    public override string OrderByKey => $"AddMessageDeclaration:{DeclaredMessageType.TypeName}";
}
