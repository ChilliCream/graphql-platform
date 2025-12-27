using HotChocolate.Execution;
using HotChocolate.Fusion.Text;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Represents a field selection during execution in the Fusion execution engine.
/// </summary>
public sealed class Selection : ISelection
{
    private readonly FieldSelectionNode[] _syntaxNodes;
    private readonly ulong[] _includeFlags;
    private readonly byte[] _utf8ResponseName;
    private Flags _flags;

    public Selection(
        int id,
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

        _utf8ResponseName = Utf8StringCache.GetUtf8String(responseName);
    }

    /// <inheritdoc />
    public int Id { get; }

    /// <inheritdoc />
    public string ResponseName { get; }

    internal ReadOnlySpan<byte> Utf8ResponseName => _utf8ResponseName;

    /// <inheritdoc />
    public bool IsInternal => (_flags & Flags.Internal) == Flags.Internal;

    /// <inheritdoc />
    public bool IsConditional => _includeFlags.Length > 0;

    /// <inheritdoc />
    public bool IsLeaf => (_flags & Flags.Leaf) == Flags.Leaf;

    /// <inheritdoc />
    public IOutputFieldDefinition Field { get; }

    /// <inheritdoc />
    public IType Type => Field.Type;

    /// <summary>
    /// Gets the selection set that contains this selection.
    /// </summary>
    public SelectionSet DeclaringSelectionSet { get; private set; } = null!;

    /// <inheritdoc />
    ISelectionSet ISelection.DeclaringSelectionSet => DeclaringSelectionSet;

    /// <summary>
    /// Gets the syntax nodes that contributed to this selection.
    /// </summary>
    public ReadOnlySpan<FieldSelectionNode> SyntaxNodes => _syntaxNodes;

    internal ResolveFieldValue? Resolver => Field.Features.Get<ResolveFieldValue>();

    IEnumerable<FieldNode> ISelection.GetSyntaxNodes()
    {
        for (var i = 0; i < SyntaxNodes.Length; i++)
        {
            yield return SyntaxNodes[i].Node;
        }
    }

    /// <inheritdoc />
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

    public override string ToString()
    {
        if (SyntaxNodes[0].Node.Alias is not null)
        {
            return $"{ResponseName} : {Field.Name}";
        }

        return Field.Name;
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
