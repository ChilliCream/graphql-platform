using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

public sealed class Selection
{
    private readonly FieldSelectionNode[] _syntaxNodes;
    private readonly ulong _includeFlags;
    private bool _isSealed;

    public Selection(
        uint id,
        string responseName,
        IOutputFieldDefinition field,
        FieldSelectionNode[] syntaxNodes,
        ulong includeFlags)
    {
        ArgumentNullException.ThrowIfNull(field);

        if (syntaxNodes.Length == 0)
        {
            throw new ArgumentException(
                "The syntaxNodes collection cannot be empty.",
                nameof(syntaxNodes));
        }

        Id = id;
        ResponseName = responseName;
        Field = field;
        _syntaxNodes = syntaxNodes;
        _includeFlags = includeFlags;
    }

    public uint Id { get; }

    public string ResponseName { get; }

    public IOutputFieldDefinition Field { get; }

    public IType Type => Field.Type;

    public SelectionSet DeclaringSelectionSet { get; private set; } = null!;

    public ReadOnlySpan<FieldSelectionNode> SyntaxNodes => _syntaxNodes;

    public bool IsIncluded(ulong includeFlags)
    {
        if (includeFlags == 0 || _includeFlags == 0)
        {
            return true;
        }

        return (_includeFlags & includeFlags) != 0;
    }

    internal void Seal(SelectionSet selectionSet)
    {
        if (_isSealed)
        {
            throw new InvalidOperationException("Selection is already sealed.");
        }

        _isSealed = true;
        DeclaringSelectionSet = selectionSet;
    }
}

public sealed record FieldSelectionNode(FieldNode Node, ulong IncludeFlags);