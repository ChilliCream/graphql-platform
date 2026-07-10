using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Represents an inline fragment with an optional type condition and its body.
/// </summary>
internal readonly struct ResultFragment(ITypeDefinition? typeCondition, ResultSelectionSet body)
{
    public ITypeDefinition? TypeCondition { get; } = typeCondition;
    public ResultSelectionSet Body { get; } = body;
}
