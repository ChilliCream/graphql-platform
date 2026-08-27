using System.Runtime.CompilerServices;
using HotChocolate.Execution;
using HotChocolate.Fusion.Text;
using HotChocolate.Fusion.Types;
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
    private readonly ulong[][]? _wideIncludeFlags;
    private readonly byte[] _utf8ResponseName;
    private readonly ulong _deferMask;
    private readonly ulong[]? _wideDeferMask;
    private readonly DeliveryGroup[] _deliveryGroups;
    private readonly ITypeDefinition _namedType;
    private readonly IType _type;
    private readonly IType _unwrappedType;
    private readonly IType? _listElementType;
    private readonly TypeKind _unwrappedKind;
    private readonly TypeKind _listElementKind;
    private Flags _flags;
    private SelectionSet? _childSelectionSet;

    public Selection(
        int id,
        string responseName,
        IOutputFieldDefinition field,
        FieldSelectionNode[] syntaxNodes,
        ulong[] includeFlags,
        bool isInternal,
        ulong[][]? wideIncludeFlags = null,
        ulong deferMask = 0,
        ulong[]? wideDeferMask = null,
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
        _wideIncludeFlags = wideIncludeFlags;
        _deferMask = deferMask;
        _wideDeferMask = wideDeferMask;
        _deliveryGroups = deliveryGroups ?? s_emptyDeliveryGroups;
        _flags = isInternal ? Flags.Internal : Flags.None;

        var type = field.Type;
        _type = type;

        var isNonNull = type.Kind is TypeKind.NonNull;
        var unwrappedType = isNonNull ? type.InnerType() : type;
        _unwrappedType = unwrappedType;
        _unwrappedKind = unwrappedType.Kind;

        if (isNonNull)
        {
            _flags |= Flags.NonNull;
        }

        var namedType = type.NamedType();
        _namedType = namedType;

        if (namedType.IsLeafType())
        {
            _flags |= Flags.Leaf;
        }

        if (namedType is FusionEnumTypeDefinition)
        {
            _flags |= Flags.EnumValue;
        }

        if (namedType is FusionObjectTypeDefinition { IsValueType: true }
            or FusionInterfaceTypeDefinition { IsValueType: true }
            or FusionUnionTypeDefinition { IsValueType: true })
        {
            _flags |= Flags.ValueTypeNamedType;
        }

        if (_unwrappedKind is TypeKind.List)
        {
            var listElementType = ((ListType)unwrappedType).ElementType;
            _listElementType = listElementType;

            var listElementKind = listElementType.Kind;

            if (listElementKind is TypeKind.NonNull)
            {
                _flags |= Flags.NonNullListElement;
                listElementKind = listElementType.InnerType().Kind;
            }

            _listElementKind = listElementKind;
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
    public bool IsEnumValue => (_flags & Flags.EnumValue) == Flags.EnumValue;

    /// <summary>
    /// Gets the named type of the selection's field type, with all list and
    /// non-null wrappers removed.
    /// </summary>
    public ITypeDefinition NamedType => _namedType;

    /// <inheritdoc />
    public IOutputFieldDefinition Field { get; }

    /// <inheritdoc />
    public IType Type => _type;

    /// <summary>
    /// Gets a value indicating whether the selection's field type is non-nullable.
    /// </summary>
    internal bool IsNonNull => (_flags & Flags.NonNull) == Flags.NonNull;

    /// <summary>
    /// Gets the selection's field type with its non-null wrapper removed.
    /// Equals <see cref="Type"/> when the field type is nullable.
    /// </summary>
    internal IType UnwrappedType => _unwrappedType;

    /// <summary>
    /// Gets the type kind of <see cref="UnwrappedType"/>.
    /// </summary>
    internal TypeKind UnwrappedKind => _unwrappedKind;

    /// <summary>
    /// Gets a value indicating whether <see cref="NamedType"/> is a value type,
    /// a type shared across source schemas that has no entity lookups.
    /// </summary>
    internal bool IsValueTypeNamedType => (_flags & Flags.ValueTypeNamedType) == Flags.ValueTypeNamedType;

    /// <summary>
    /// Gets the element type of the first list level when <see cref="UnwrappedKind"/>
    /// is <see cref="TypeKind.List"/>; otherwise, <c>null</c>. The element type
    /// keeps its non-null wrapper.
    /// </summary>
    internal IType? ListElementType => _listElementType;

    /// <summary>
    /// Gets the type kind of the first list level's element type after removing
    /// its non-null wrapper. Only meaningful when <see cref="UnwrappedKind"/> is
    /// <see cref="TypeKind.List"/>.
    /// </summary>
    internal TypeKind ListElementKind => _listElementKind;

    /// <summary>
    /// Gets a value indicating whether the first list level's element type is
    /// non-nullable. Only meaningful when <see cref="UnwrappedKind"/> is
    /// <see cref="TypeKind.List"/>.
    /// </summary>
    internal bool IsNonNullListElement => (_flags & Flags.NonNullListElement) == Flags.NonNullListElement;

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
    public SelectionSet? GetSelectionSet(IComplexTypeDefinition typeContext)
    {
        if ((_flags & Flags.Leaf) == Flags.Leaf)
        {
            return null;
        }

        // _childSelectionSet is only set for non-leaf fields whose named type is a concrete object type.
        // When it is set, we can return it right away because that child selection set is plan-stable.
        // A concurrent recompute produces the same instance, so no synchronization is needed.
        var childSelectionSet = _childSelectionSet;

        if (childSelectionSet is not null)
        {
            return childSelectionSet;
        }

        if (_namedType is IObjectTypeDefinition)
        {
            childSelectionSet = DeclaringSelectionSet.DeclaringOperation.GetSelectionSet(this, typeContext);
            _childSelectionSet = childSelectionSet;
            return childSelectionSet;
        }

        return DeclaringSelectionSet.DeclaringOperation.GetSelectionSet(this, typeContext);
    }

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

        if ((_flags & Flags.RequiresWideIncludeFlags) != 0)
        {
            throw new InvalidOperationException(
                "The operation has more than 64 include conditions; this check requires "
                + "the wide include flags. Use IsIncluded(ulong, ReadOnlySpan<ulong>).");
        }

        return IsIncludedUnchecked(includeFlags);
    }

    /// <inheritdoc cref="ISelection.IsIncluded(ulong, ReadOnlySpan{ulong})" />
    public bool IsIncluded(ulong includeFlags, ReadOnlySpan<ulong> wideIncludeFlags)
        => IsIncludedWide(includeFlags, wideIncludeFlags);

    /// <summary>
    /// Evaluates the include conditions against word 0 of the request flags only.
    /// This is the narrow fast path; callers must ensure the operation has at most
    /// 64 include conditions.
    /// </summary>
    internal bool IsIncludedUnchecked(ulong includeFlags)
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

    /// <summary>
    /// Evaluates the include conditions against all words of the request flags.
    /// A path is satisfied when every word of its mask is fully covered by the
    /// corresponding request word; the selection is included when any path is satisfied.
    /// </summary>
    internal bool IsIncludedWide(ulong includeFlags, ReadOnlySpan<ulong> wideIncludeFlags)
    {
        if (_includeFlags.Length == 0)
        {
            return true;
        }

        var wide = _wideIncludeFlags;

        for (var i = 0; i < _includeFlags.Length; i++)
        {
            var flags = _includeFlags[i];

            if ((flags & includeFlags) != flags)
            {
                continue;
            }

            var overflow = wide?[i];

            if (overflow is null || overflow.Length == 0)
            {
                return true;
            }

            var satisfied = true;

            for (var w = 0; w < overflow.Length; w++)
            {
                var pathWord = overflow[w];
                var requestWord = w < wideIncludeFlags.Length ? wideIncludeFlags[w] : 0ul;

                if ((pathWord & requestWord) != pathWord)
                {
                    satisfied = false;
                    break;
                }
            }

            if (satisfied)
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

        // Conditional (resp. deferrable) selections of a wide operation must be
        // evaluated with the wide flag overloads; the narrow checks throw for them.
        var operation = selectionSet.DeclaringOperation;

        if (operation.HasWideIncludeFlags && _includeFlags.Length > 0)
        {
            _flags |= Flags.RequiresWideIncludeFlags;
        }

        if (operation.HasWideDeferFlags && _deliveryGroups.Length > 0)
        {
            _flags |= Flags.RequiresWideDeferFlags;
        }

        _flags |= Flags.Sealed;
        DeclaringSelectionSet = selectionSet;
    }

    /// <inheritdoc />
    public bool IsDeferred(ulong deferFlags)
    {
        if ((_flags & Flags.RequiresWideDeferFlags) != 0)
        {
            throw new InvalidOperationException(
                "The operation has more than 64 defer conditions; this check requires "
                + "the wide defer flags. Use IsDeferred(ulong, ReadOnlySpan<ulong>).");
        }

        return IsDeferredUnchecked(deferFlags);
    }

    /// <inheritdoc cref="ISelection.IsDeferred(ulong, ReadOnlySpan{ulong})" />
    public bool IsDeferred(ulong deferFlags, ReadOnlySpan<ulong> wideDeferFlags)
        => IsDeferredWide(deferFlags, wideDeferFlags);

    /// <summary>
    /// Evaluates the defer mask against word 0 of the request flags only.
    /// This is the narrow fast path; callers must ensure the operation has at most
    /// 64 defer conditions.
    /// </summary>
    internal bool IsDeferredUnchecked(ulong deferFlags) => (_deferMask & deferFlags) != 0;

    /// <summary>
    /// Evaluates the defer mask against all words of the request flags.
    /// The selection is deferred when any bit matches in any word.
    /// </summary>
    internal bool IsDeferredWide(ulong deferFlags, ReadOnlySpan<ulong> wideDeferFlags)
    {
        if ((_deferMask & deferFlags) != 0)
        {
            return true;
        }

        var wide = _wideDeferMask;

        if (wide is null)
        {
            return false;
        }

        var length = Math.Min(wide.Length, wideDeferFlags.Length);

        for (var i = 0; i < length; i++)
        {
            if ((wide[i] & wideDeferFlags[i]) != 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets a value indicating whether this selection can be deferred for some request.
    /// </summary>
    internal bool CanBeDeferred => _deferMask != 0 || _wideDeferMask is not null;

    /// <summary>
    /// Returns the active delivery groups for this selection after resolving
    /// each occurrence to its nearest active ancestor and pruning child groups
    /// whose parent is also active. Returns <c>null</c> when the selection
    /// belongs to the initial result.
    /// </summary>
    public DeliveryGroup[]? GetActiveDeliveryGroups(ulong deferFlags)
    {
        if ((_flags & Flags.RequiresWideDeferFlags) != 0)
        {
            throw new InvalidOperationException(
                "The operation has more than 64 defer conditions; this check requires "
                + "the wide defer flags. Use GetActiveDeliveryGroups(ulong, ReadOnlySpan<ulong>).");
        }

        return GetActiveDeliveryGroups(deferFlags, default);
    }

    /// <summary>
    /// Returns the active delivery groups for this selection, evaluating all words
    /// of the request defer flags.
    /// </summary>
    /// <param name="deferFlags">The defer condition flags for condition indexes 0-63.</param>
    /// <param name="wideDeferFlags">
    /// The overflow words for condition indexes 64 and above; empty for narrow operations.
    /// </param>
    /// <returns>
    /// The active delivery groups (pruned), or <c>null</c> when the selection
    /// belongs to the initial result.
    /// </returns>
    public DeliveryGroup[]? GetActiveDeliveryGroups(ulong deferFlags, ReadOnlySpan<ulong> wideDeferFlags)
    {
        if (_deliveryGroups.Length == 0)
        {
            return null;
        }

        if (_deliveryGroups.Length == 1)
        {
            var active = ResolveActiveAncestor(_deliveryGroups[0], deferFlags, wideDeferFlags);
            return active is null ? null : [active];
        }

        DeliveryGroup[]? result = null;
        var count = 0;

        for (var i = 0; i < _deliveryGroups.Length; i++)
        {
            var effective = ResolveActiveAncestor(_deliveryGroups[i], deferFlags, wideDeferFlags);

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
        if ((_flags & Flags.RequiresWideDeferFlags) != 0)
        {
            throw new InvalidOperationException(
                "The operation has more than 64 defer conditions; this check requires "
                + "the wide defer flags. Use HasActiveDeliveryGroup(ulong, ReadOnlySpan<ulong>, DeliveryGroup).");
        }

        return HasActiveDeliveryGroup(deferFlags, default, target);
    }

    /// <summary>
    /// Determines whether <paramref name="target"/> is the nearest active delivery
    /// group for any occurrence of this selection, evaluating all words of the
    /// request defer flags. Returns <c>false</c> if any occurrence belongs to the
    /// initial result.
    /// </summary>
    /// <param name="deferFlags">The defer condition flags for condition indexes 0-63.</param>
    /// <param name="wideDeferFlags">
    /// The overflow words for condition indexes 64 and above; empty for narrow operations.
    /// </param>
    /// <param name="target">The delivery group to look for.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="target"/> is an active delivery group of this selection.
    /// </returns>
    public bool HasActiveDeliveryGroup(ulong deferFlags, ReadOnlySpan<ulong> wideDeferFlags, DeliveryGroup target)
    {
        if (_deliveryGroups.Length == 0)
        {
            return false;
        }

        var found = false;

        for (var i = 0; i < _deliveryGroups.Length; i++)
        {
            var effective = ResolveActiveAncestor(_deliveryGroups[i], deferFlags, wideDeferFlags);

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
    private static DeliveryGroup? ResolveActiveAncestor(
        DeliveryGroup start,
        ulong deferFlags,
        ReadOnlySpan<ulong> wideDeferFlags)
    {
        var deliveryGroup = start;

        while (deliveryGroup is not null)
        {
            if (IsConditionBitSet(deferFlags, wideDeferFlags, deliveryGroup.DeferConditionIndex))
            {
                return deliveryGroup;
            }

            deliveryGroup = deliveryGroup.Parent;
        }

        return null;
    }

    /// <summary>
    /// Tests the request flag bit for a condition index. Word 0 is the
    /// <paramref name="flags"/> parameter; higher words come from the overflow span.
    /// </summary>
    private static bool IsConditionBitSet(ulong flags, ReadOnlySpan<ulong> wideFlags, int index)
    {
        var word = index >> 6;
        var bit = 1ul << (index & 63);

        if (word == 0)
        {
            return (flags & bit) != 0;
        }

        word--;
        return (uint)word < (uint)wideFlags.Length && (wideFlags[word] & bit) != 0;
    }

    [Flags]
    private enum Flags
    {
        None = 0,
        Internal = 1,
        Leaf = 2,
        EnumValue = 4,
        Sealed = 8,
        NonNull = 16,
        ValueTypeNamedType = 32,
        NonNullListElement = 64,
        RequiresWideIncludeFlags = 128,
        RequiresWideDeferFlags = 256
    }
}
