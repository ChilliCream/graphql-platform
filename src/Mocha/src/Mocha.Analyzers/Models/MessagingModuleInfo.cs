namespace Mocha.Analyzers;

/// <summary>
/// Represents the metadata for a <c>[assembly: MessagingModule("...")]</c> attribute
/// discovered during source generation.
/// </summary>
/// <param name="ModuleName">The module name specified in the attribute.</param>
/// <param name="JsonContextTypeName">
/// The fully qualified type name of the <c>JsonSerializerContext</c> specified via
/// the <c>JsonContext</c> named property, or <c>null</c> if not specified.
/// </param>
public sealed record MessagingModuleInfo(string ModuleName, string? JsonContextTypeName = null) : SyntaxInfo
{
    /// <inheritdoc />
    public override string OrderByKey => $"MsgModule:{ModuleName}";
}
