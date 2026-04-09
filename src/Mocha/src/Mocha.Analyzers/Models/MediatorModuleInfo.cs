namespace Mocha.Analyzers;

/// <summary>
/// Represents the metadata for a <c>[assembly: MediatorModule("...")]</c> attribute
/// discovered during source generation.
/// </summary>
/// <param name="ModuleName">The module name specified in the attribute.</param>
public sealed record MediatorModuleInfo(string ModuleName) : SyntaxInfo
{
    /// <inheritdoc />
    public override string OrderByKey => ModuleName;
}
