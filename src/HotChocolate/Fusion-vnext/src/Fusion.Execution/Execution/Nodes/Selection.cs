using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class Selection
{
    private readonly FieldSelectionNode[] _syntaxNodes;
    private readonly ulong[] _includeFlags;
    private Flags _flags;

    public Selection(
        uint id,
        string responseName,
        IOutputFieldDefinition field,
        FieldSelectionNode[] syntaxNodes,
        ulong[] includeFlags,
        bool isInternal)
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
        _flags = isInternal ? Flags.Internal : Flags.None;

        if (field.Type.NamedType().IsLeafType())
        {
            _flags |= Flags.Leaf;
        }
    }

    public uint Id { get; }

    public string ResponseName { get; }

    public bool IsInternal => (_flags & Flags.Internal) == Flags.Internal;

    public bool IsLeaf => (_flags & Flags.Leaf) == Flags.Leaf;

    public IOutputFieldDefinition Field { get; }

    public IType Type => Field.Type;

    public SelectionSet DeclaringSelectionSet { get; private set; } = null!;

    public ReadOnlySpan<FieldSelectionNode> SyntaxNodes => _syntaxNodes;

    internal ResolveFieldValue? Resolver => Field.Features.Get<ResolveFieldValue>();

    public bool IsIncluded(ulong includeFlags)
    {
        if (_includeFlags.Length == 0)
        {
            return true;
        }

        if (_includeFlags.Length == 1)
        {
            var flags1 = _includeFlags[0];
            return (flags1 & includeFlags) == flags1;
        }

        if (_includeFlags.Length == 2)
        {
            var flags1 = _includeFlags[0];
            var flags2 = _includeFlags[1];
            return (flags1 & includeFlags) == flags1 || (flags2 & includeFlags) == flags2;
        }

        if (_includeFlags.Length == 3)
        {
            var flags1 = _includeFlags[0];
            var flags2 = _includeFlags[1];
            var flags3 = _includeFlags[2];
            return (flags1 & includeFlags) == flags1
                || (flags2 & includeFlags) == flags2
                || (flags3 & includeFlags) == flags3;
        }

        var span = _includeFlags.AsSpan();

        for (var i = 0; i < span.Length; i++)
        {
            if ((span[i] & includeFlags) == span[i])
            {
                return true;
            }
        }

        return false;
    }

    internal void Seal(SelectionSet selectionSet)
    {
        if ((_flags & Flags.Sealed) == Flags.Sealed)
        {
            throw new InvalidOperationException("Selection is already sealed.");
        }

        _flags |= Flags.Sealed;
        DeclaringSelectionSet = selectionSet;
    }

    [Flags]
    private enum Flags
    {
        None = 0,
        Internal = 1,
        Leaf = 2,
        Sealed = 4
    }
}
