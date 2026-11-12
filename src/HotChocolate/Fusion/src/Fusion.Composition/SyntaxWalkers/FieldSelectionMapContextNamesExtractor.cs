using System.Collections.Immutable;
using HotChocolate.Fusion.Language;

namespace HotChocolate.Fusion.SyntaxWalkers;

/// <summary>
/// Extracts the names of the contexts that a field selection map references.
/// </summary>
internal sealed class FieldSelectionMapContextNamesExtractor()
    : FieldSelectionMapSyntaxVisitor<List<string>>(Continue)
{
    public static readonly FieldSelectionMapContextNamesExtractor Instance = new();

    /// <summary>
    /// Returns the distinct context names in order of first appearance.
    /// </summary>
    public ImmutableArray<string> Extract(IValueSelectionNode selectedValue)
    {
        var names = new List<string>();

        Visit(selectedValue, names);

        return [.. names.Distinct()];
    }

    protected override ISyntaxVisitorAction Enter(PathNode node, List<string> context)
    {
        if (node.ContextName is { } contextName)
        {
            context.Add(contextName.Value);
        }

        return Continue;
    }
}
