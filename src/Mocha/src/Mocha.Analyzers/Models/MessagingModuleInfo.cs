namespace Mocha.Analyzers;

/// <summary>
/// Represents the metadata for a <c>[assembly: MessagingModule("...")]</c> attribute
/// discovered during source generation.
/// </summary>
/// <param name="ModuleName">The module name specified in the attribute.</param>
public sealed record MessagingModuleInfo(string ModuleName) : SyntaxInfo
{
    /// <inheritdoc />
    public override string OrderByKey => $"MsgModule:{ModuleName}";
}
