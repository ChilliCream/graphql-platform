using System.Runtime.CompilerServices;
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
    private static readonly DeliveryGroup[] s_emptyDeliveryGroups = [];

    private readonly FieldSelectionNode[] _syntaxNodes;
    private readonly ulong[] _includeFlags;
    private readonly byte[] _utf8ResponseName;
    private readonly ulong _deferMask;
    private readonly DeliveryGroup[] _deliveryGroups;
    private Flags _flags;

    public Selection(
        int id,
        string responseName,
        IOutputFieldDefinition field,
        FieldSelectionNode[] syntaxNodes,
        ulong[] includeFlags,
        bool isInternal,
        ulong deferMask = 0,
        DeliveryGroup[]? deliveryGroups = null)
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
        _deferMask = deferMask;
        _deliveryGroups = deliveryGroups ?? s_emptyDeliveryGroups;
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
    /// Gets the child selection set for this selection's named return type.
    /// </summary>
    /// <returns>
    /// The child selection set, or <c>null</c> when this selection has no child
    /// selection set.
    /// </returns>
    public SelectionSet? GetSelectionSet()
        => IsLeaf ? null : DeclaringSelectionSet.DeclaringOperation.GetSelectionSet(this);

    /// <summary>
    /// Gets the child selection set for this selection and the specified
    /// <paramref name="typeContext"/>.
    /// </summary>
    /// <returns>
    /// The child selection set, or <c>null</c> when this selection has no child
    /// selection set.
    /// </returns>
    public SelectionSet? GetSelectionSet(IObjectTypeDefinition typeContext)
        => IsLeaf ? null : DeclaringSelectionSet.DeclaringOperation.GetSelectionSet(this, typeContext);

    /// <summary>
    /// Gets the syntax nodes that contributed to this selection.
    /// </summary>
    public ReadOnlySpan<FieldSelectionNode> SyntaxNodes => _syntaxNodes;

    internal ResolveFieldValue? Resolver => Field.Features.Get<ResolveFieldValue>();

    internal AsyncResolveFieldValue? AsyncResolver => Field.Features.Get<AsyncResolveFieldValue>();

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

        var includeFlagsArray = _includeFlags;

        for (var i = 0; i < includeFlagsArray.Length; i++)
        {
            var current = includeFlagsArray[i];

            if ((current & includeFlags) == current)
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

    public bool IsDeferred(ulong deferFlags) => (_deferMask & deferFlags) != 0;

    /// <summary>
    /// Returns the active delivery groups for this selection after resolving
    /// each occurrence to its nearest active ancestor and pruning child groups
    /// whose parent is also active. Returns <c>null</c> when the selection
    /// belongs to the initial result.
    /// </summary>
    public DeliveryGroup[]? GetActiveDeliveryGroups(ulong deferFlags)
    {
        if (_deliveryGroups.Length == 0)
        {
            return null;
        }

        if (_deliveryGroups.Length == 1)
        {
            var active = ResolveActiveAncestor(_deliveryGroups[0], deferFlags);
            return active is null ? null : [active];
        }

        DeliveryGroup[]? result = null;
        var count = 0;

        for (var i = 0; i < _deliveryGroups.Length; i++)
        {
            var effective = ResolveActiveAncestor(_deliveryGroups[i], deferFlags);

            if (effective is null)
            {
                // One occurrence is non-deferred; the field is non-deferred overall.
                return null;
            }

            var duplicate = false;
            if (result is not null)
            {
                for (var j = 0; j < count; j++)
                {
                    if (result[j] == effective)
                    {
                        duplicate = true;
                        break;
                    }
                }
            }

            if (!duplicate)
            {
                result ??= new DeliveryGroup[_deliveryGroups.Length];
                result[count++] = effective;
            }
        }

        if (result is null || count == 0)
        {
            return null;
        }

        // Parent-child pruning: if a parent and child are both in the set,
        // keep only the outermost.
        for (var i = count - 1; i >= 0; i--)
        {
            var ancestor = result[i].Parent;

            while (ancestor is not null)
            {
                for (var j = 0; j < count; j++)
                {
                    if (j != i && result[j] == ancestor)
                    {
                        result[i] = result[--count];
                        goto nextItem;
                    }
                }

                ancestor = ancestor.Parent;
            }

nextItem:
            ;
        }

        if (count == 0)
        {
            return null;
        }

        if (count < result.Length)
        {
            Array.Resize(ref result, count);
        }

        return result;
    }

    /// <summary>
    /// Determines whether <paramref name="target"/> is the nearest active
    /// delivery group for any occurrence of this selection under the specified
    /// <paramref name="deferFlags"/>. Returns <c>false</c> if any occurrence
    /// belongs to the initial result.
    /// </summary>
    public bool HasActiveDeliveryGroup(ulong deferFlags, DeliveryGroup target)
    {
        if (_deliveryGroups.Length == 0)
        {
            return false;
        }

        var found = false;

        for (var i = 0; i < _deliveryGroups.Length; i++)
        {
            var effective = ResolveActiveAncestor(_deliveryGroups[i], deferFlags);

            if (effective is null)
            {
                // Any non-deferred occurrence makes the whole field non-deferred.
                return false;
            }

            if (effective == target)
            {
                found = true;
            }
        }

        return found;
    }

    // Returns the nearest active delivery group in the @defer parent chain.
    // If none are active, the field occurrence belongs to the initial result.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static DeliveryGroup? ResolveActiveAncestor(DeliveryGroup start, ulong deferFlags)
    {
        var deliveryGroup = start;

        while (deliveryGroup is not null)
        {
            if ((deferFlags & (1UL << deliveryGroup.DeferConditionIndex)) != 0)
            {
                return deliveryGroup;
            }

            deliveryGroup = deliveryGroup.Parent;
        }

        return null;
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
