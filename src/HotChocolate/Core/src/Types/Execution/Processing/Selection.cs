using HotChocolate.Properties;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// Represents a field selection during execution.
/// </summary>
public sealed class Selection : ISelection, IFeatureProvider
{
    private static readonly ArgumentMap s_emptyArguments = ArgumentMap.Empty;
    private readonly FieldSelectionNode[] _syntaxNodes;
    private readonly ulong[] _includeFlags;
    private readonly ulong[]? _wideIncludeFlags;
    private readonly int _wideIncludeFlagsStride;
    private readonly byte[] _utf8ResponseName;
    private readonly DeferUsage[] _deferUsage;
    private readonly ulong _deferMask;
    private readonly ulong[]? _wideDeferMask;
    private Flags _flags;
    private SelectionSet? _declaringSelectionSet;

    internal Selection(
        int id,
        string responseName,
        SelectionPath fieldSelectionPath,
        ObjectField field,
        FieldSelectionNode[] syntaxNodes,
        ulong[] includeFlags,
        bool isProjectionRequirement,
        ulong[]? wideIncludeFlags = null,
        int wideIncludeFlagsStride = 0,
        DeferUsage[]? deferUsage = null,
        ulong deferMask = 0,
        ulong[]? wideDeferMask = null,
        bool isInternal = false,
        ArgumentMap? arguments = null,
        FieldDelegate? resolverPipeline = null,
        PureFieldDelegate? pureResolver = null,
        BatchFieldDelegate? batchResolverPipeline = null)
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
        FieldSelectionPath = fieldSelectionPath;
        Field = field;
        Type = field.Type;
        Arguments = arguments ?? s_emptyArguments;
        ResolverPipeline = resolverPipeline;
        PureResolver = pureResolver;
        BatchResolverPipeline = batchResolverPipeline;
        Strategy = InferStrategy(
            isSerial: !field.IsParallelExecutable,
            hasPureResolver: pureResolver is not null,
            hasBatchResolver: batchResolverPipeline is not null);
        _syntaxNodes = syntaxNodes;
        _includeFlags = includeFlags;
        _wideIncludeFlags = wideIncludeFlags;
        _wideIncludeFlagsStride = wideIncludeFlagsStride;
        _deferUsage = deferUsage ?? [];
        _deferMask = deferMask;
        _wideDeferMask = wideDeferMask;
        _flags = isInternal ? Flags.Internal : Flags.None;

        if (isProjectionRequirement)
        {
            _flags |= Flags.ProjectionRequirement;
        }

        if (field.Type.NamedType().IsLeafType())
        {
            _flags |= Flags.Leaf;
        }

        if (field.Type.IsListType())
        {
            _flags |= Flags.List;
        }

        _utf8ResponseName = Utf8StringCache.GetUtf8String(responseName);
    }

    private Selection(
        int id,
        string responseName,
        byte[] utf8ResponseName,
        SelectionPath fieldSelectionPath,
        ObjectField field,
        IType type,
        FieldSelectionNode[] syntaxNodes,
        ulong[] includeFlags,
        ulong[]? wideIncludeFlags,
        int wideIncludeFlagsStride,
        DeferUsage[] deferUsage,
        ulong deferMask,
        ulong[]? wideDeferMask,
        Flags flags,
        ArgumentMap? arguments,
        SelectionExecutionStrategy strategy,
        FieldDelegate? resolverPipeline,
        PureFieldDelegate? pureResolver,
        BatchFieldDelegate? batchResolverPipeline)
    {
        Id = id;
        ResponseName = responseName;
        FieldSelectionPath = fieldSelectionPath;
        Field = field;
        Type = type;
        Arguments = arguments ?? s_emptyArguments;
        ResolverPipeline = resolverPipeline;
        PureResolver = pureResolver;
        BatchResolverPipeline = batchResolverPipeline;
        Strategy = strategy;
        _syntaxNodes = syntaxNodes;
        _includeFlags = includeFlags;
        _wideIncludeFlags = wideIncludeFlags;
        _wideIncludeFlagsStride = wideIncludeFlagsStride;
        _deferUsage = deferUsage;
        _deferMask = deferMask;
        _wideDeferMask = wideDeferMask;
        _flags = flags;
        _utf8ResponseName = utf8ResponseName;
    }

    /// <inheritdoc />
    public int Id { get; }

    /// <inheritdoc />
    public string ResponseName { get; }

    internal ReadOnlySpan<byte> Utf8ResponseName => _utf8ResponseName;

    public SelectionPath FieldSelectionPath { get; }

    /// <inheritdoc />
    public bool IsInternal => (_flags & Flags.Internal) == Flags.Internal;

    internal bool IsProjectionRequirement
        => (_flags & Flags.ProjectionRequirement) == Flags.ProjectionRequirement;

    /// <inheritdoc />
    public bool IsConditional => _includeFlags.Length > 0;

    internal ulong IncludeConditionMask
    {
        get
        {
            if (_includeFlags.Length == 0)
            {
                return 0;
            }

            if (_includeFlags.Length == 1)
            {
                return _includeFlags[0];
            }

            var mask = 0UL;
            var includeFlags = _includeFlags.AsSpan();

            for (var i = 0; i < includeFlags.Length; i++)
            {
                mask |= includeFlags[i];
            }

            return mask;
        }
    }

    /// <summary>
    /// Gets a value indicating whether this selection returns a list type.
    /// </summary>
    public bool IsList => (_flags & Flags.List) == Flags.List;

    /// <inheritdoc />
    public bool IsLeaf => (_flags & Flags.Leaf) == Flags.Leaf;

    /// <summary>
    /// Gets a value indicating whether this selection has child selections.
    /// </summary>
    public bool HasSelections => !IsLeaf;

    /// <summary>
    /// Gets the field definition from the schema that this selection targets.
    /// </summary>
    public ObjectField Field { get; }

    /// <inheritdoc />
    IOutputFieldDefinition ISelection.Field => Field;

    /// <inheritdoc />
    public IType Type { get; }

    /// <summary>
    /// Gets the object type that declares the field being selected.
    /// </summary>
    public ObjectType DeclaringType => Field.DeclaringType;

    /// <summary>
    /// Gets the selection set that contains this selection.
    /// </summary>
    public SelectionSet DeclaringSelectionSet
        => _declaringSelectionSet ?? throw ThrowHelper.Selection_NotFullyInitialized();

    /// <inheritdoc />
    ISelectionSet ISelection.DeclaringSelectionSet => DeclaringSelectionSet;

    /// <summary>
    /// Gets the operation that contains this selection.
    /// </summary>
    public Operation DeclaringOperation => DeclaringSelectionSet.DeclaringOperation;

    /// <summary>
    /// Gets the selection features.
    /// </summary>
    public SelectionFeatureCollection Features => new(DeclaringOperation.Features, Id);

    IFeatureCollection IFeatureProvider.Features => Features;

    /// <summary>
    /// Gets the arguments that were provided to this field selection.
    /// </summary>
    public ArgumentMap Arguments { get; }

    /// <summary>
    /// Gets the execution strategy for this selection.
    /// </summary>
    public SelectionExecutionStrategy Strategy { get; private set; }

    /// <summary>
    /// Gets the resolver pipeline delegate for this selection.
    /// </summary>
    public FieldDelegate? ResolverPipeline { get; private set; }

    /// <summary>
    /// Gets the pure resolver delegate for this selection.
    /// </summary>
    public PureFieldDelegate? PureResolver { get; private set; }

    /// <summary>
    /// Gets the batch resolver pipeline delegate for this selection.
    /// When set, the field is resolved using a batch pipeline that receives
    /// multiple parent contexts in a single invocation.
    /// </summary>
    public BatchFieldDelegate? BatchResolverPipeline { get; private set; }

    /// <summary>
    /// Gets the syntax nodes that contributed to this selection.
    /// </summary>
    public ReadOnlySpan<FieldSelectionNode> SyntaxNodes => _syntaxNodes;

    IEnumerable<FieldNode> ISelection.GetSyntaxNodes()
    {
        for (var i = 0; i < SyntaxNodes.Length; i++)
        {
            yield return SyntaxNodes[i].Node;
        }
    }

    /// <summary>
    /// Gets the selection set for this selection resolved against the specified object type.
    /// </summary>
    /// <param name="typeContext">
    /// The object type context to resolve the selection set against.
    /// </param>
    /// <returns>
    /// The selection set containing the child selections for the specified type context.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this selection is a leaf selection (scalar or enum) which does not have child selections.
    /// </exception>
    public SelectionSet GetSelectionSet(ObjectType typeContext)
    {
        if (IsLeaf)
        {
            throw new InvalidOperationException("Leaf selections do not have a selection set.");
        }

        return DeclaringOperation.GetSelectionSet(this, typeContext);
    }

    /// <summary>
    /// Determines whether this selection should be skipped based on conditional flags.
    /// </summary>
    /// <param name="includeFlags">The conditional inclusion flags.</param>
    /// <returns>
    /// <c>true</c> if this selection should be included; otherwise, <c>false</c>.
    /// </returns>
    [Obsolete("Use IsSkipped(ConditionFlags) instead. This overload throws for operations with more than 64 conditions.")]
    public bool IsSkipped(ulong includeFlags)
        => !IsIncludedNarrow(includeFlags);

    public bool IsSkipped(ConditionFlags includeFlags)
        => !IsIncludedWide(includeFlags.Word0, includeFlags.Overflow);

    /// <summary>
    /// Determines whether this selection should be skipped based on conditional flags,
    /// including the overflow words of operations with more than 64 include conditions.
    /// </summary>
    /// <param name="includeFlags">The conditional inclusion flags for condition indexes 0-63.</param>
    /// <param name="wideIncludeFlags">
    /// The overflow words for condition indexes 64 and above; empty for narrow operations.
    /// </param>
    /// <returns>
    /// <c>true</c> if this selection should be skipped; otherwise, <c>false</c>.
    /// </returns>
    internal bool IsSkipped(ulong includeFlags, ReadOnlySpan<ulong> wideIncludeFlags)
        => !IsIncludedWide(includeFlags, wideIncludeFlags);

    /// <inheritdoc />
    [Obsolete("Use IsIncluded(ConditionFlags) instead. This overload throws for operations with more than 64 conditions.")]
    public bool IsIncluded(ulong includeFlags)
        => IsIncludedNarrow(includeFlags);

    internal bool IsIncludedNarrow(ulong includeFlags)
    {
        if (_includeFlags.Length == 0)
        {
            return true;
        }

        if ((_flags & Flags.RequiresWideIncludeFlags) != 0)
        {
            throw new InvalidOperationException(
                "The operation has more than 64 include conditions; this check requires "
                + "the wide include flags. Use IsIncluded(ConditionFlags).");
        }

        return IsIncludedUnchecked(includeFlags);
    }

    public bool IsIncluded(ConditionFlags includeFlags)
        => IsIncludedWide(includeFlags.Word0, includeFlags.Overflow);

    internal bool IsIncluded(ulong includeFlags, ReadOnlySpan<ulong> wideIncludeFlags)
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
        var stride = _wideIncludeFlagsStride;

        for (var i = 0; i < _includeFlags.Length; i++)
        {
            var flags = _includeFlags[i];

            if ((flags & includeFlags) != flags)
            {
                continue;
            }

            if (wide is null)
            {
                return true;
            }

            var satisfied = true;
            var offset = i * stride;

            for (var w = 0; w < stride; w++)
            {
                var pathWord = wide[offset + w];
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

    /// <summary>
    /// Gets a value indicating whether this selection has any defer usage.
    /// </summary>
    internal bool HasDeferUsage => _deferUsage.Length > 0;

    /// <inheritdoc />
    [Obsolete("Use IsDeferred(ConditionFlags) instead. This overload throws for operations with more than 64 conditions.")]
    public bool IsDeferred(ulong deferFlags)
        => IsDeferredNarrow(deferFlags);

    internal bool IsDeferredNarrow(ulong deferFlags)
    {
        if ((_flags & Flags.RequiresWideDeferFlags) != 0)
        {
            throw new InvalidOperationException(
                "The operation has more than 64 defer conditions; this check requires "
                + "the wide defer flags. Use IsDeferred(ConditionFlags).");
        }

        return IsDeferredUnchecked(deferFlags);
    }

    public bool IsDeferred(ConditionFlags deferFlags)
        => IsDeferredWide(deferFlags.Word0, deferFlags.Overflow);

    internal bool IsDeferred(ulong deferFlags, ReadOnlySpan<ulong> wideDeferFlags)
        => IsDeferredWide(deferFlags, wideDeferFlags);

    /// <summary>
    /// Evaluates the defer mask against word 0 of the request flags only.
    /// This is the narrow fast path; callers must ensure the operation has at most
    /// 64 defer conditions.
    /// </summary>
    internal bool IsDeferredUnchecked(ulong deferFlags)
        => _deferMask != 0 && (_deferMask & deferFlags) != 0;

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
    /// Determines whether this selection is deferred relative to a parent defer usage.
    /// </summary>
    /// <param name="deferFlags">
    /// The defer condition flags representing which <c>@defer</c> directives are active
    /// for the current request, computed from the runtime variable values of the
    /// <c>if</c> arguments on <c>@defer</c> directives.
    /// </param>
    /// <param name="parentDeferUsage">
    /// The defer usage of the parent context, or <c>null</c> if the parent is not deferred.
    /// When provided, this selection is only considered deferred if its primary defer usage
    /// matches the given parent, ensuring that the selection is delivered in the correct
    /// incremental payload.
    /// </param>
    /// <returns>
    /// <c>true</c> if this selection is deferred and belongs to the specified parent
    /// defer context; otherwise, <c>false</c>.
    /// </returns>
    [Obsolete("Use IsDeferred(ConditionFlags, DeferUsage?) instead. This overload throws for operations with more than 64 conditions.")]
    public bool IsDeferred(ulong deferFlags, DeferUsage? parentDeferUsage)
    {
        if ((_flags & Flags.RequiresWideDeferFlags) != 0)
        {
            throw new InvalidOperationException(
                "The operation has more than 64 defer conditions; this check requires "
                + "the wide defer flags. Use IsDeferred(ConditionFlags, DeferUsage?).");
        }

        return IsDeferred(deferFlags, default(ReadOnlySpan<ulong>), parentDeferUsage);
    }

    /// <summary>
    /// Determines whether this selection is deferred relative to a parent defer usage,
    /// evaluating all words of the request defer flags.
    /// </summary>
    /// <param name="deferFlags">The defer condition flags for condition indexes 0-63.</param>
    /// <param name="parentDeferUsage">
    /// The defer usage of the parent context, or <c>null</c> if the parent is not deferred.
    /// </param>
    /// <returns>
    /// <c>true</c> if this selection is deferred and belongs to the specified parent
    /// defer context; otherwise, <c>false</c>.
    /// </returns>
    public bool IsDeferred(ConditionFlags deferFlags, DeferUsage? parentDeferUsage)
        => IsDeferred(deferFlags.Word0, deferFlags.Overflow, parentDeferUsage);

    internal bool IsDeferred(ulong deferFlags, ReadOnlySpan<ulong> wideDeferFlags, DeferUsage? parentDeferUsage)
    {
        if (IsDeferredWide(deferFlags, wideDeferFlags))
        {
            if (parentDeferUsage is null)
            {
                return true;
            }

            // If the parent's defer usage is in this selection's active defer usage set,
            // this selection belongs to the parent's context and does not need to be
            // deferred separately.
            if (HasActiveDeferUsage(deferFlags, wideDeferFlags, parentDeferUsage))
            {
                return false;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the primary defer usage for this selection given the active defer flags.
    /// The primary defer usage determines which execution branch the selection belongs to.
    /// If multiple defer usages are active and one is a parent of another, the parent takes precedence.
    /// </summary>
    /// <param name="deferFlags">The active defer flags.</param>
    /// <returns>
    /// The primary defer usage, or <c>null</c> if the selection is not deferred or has no active defer usages.
    /// </returns>
    [Obsolete("Use GetPrimaryDeferUsage(ConditionFlags) instead. This overload throws for operations with more than 64 conditions.")]
    public DeferUsage? GetPrimaryDeferUsage(ulong deferFlags)
    {
        if ((_flags & Flags.RequiresWideDeferFlags) != 0)
        {
            throw new InvalidOperationException(
                "The operation has more than 64 defer conditions; this check requires "
                + "the wide defer flags. Use GetPrimaryDeferUsage(ConditionFlags).");
        }

        return GetPrimaryDeferUsage(deferFlags, default);
    }

    /// <summary>
    /// Gets the primary defer usage for this selection, evaluating all words of the
    /// request defer flags.
    /// </summary>
    /// <param name="deferFlags">The defer condition flags for condition indexes 0-63.</param>
    /// <returns>
    /// The primary defer usage, or <c>null</c> if the selection is not deferred or has no active defer usages.
    /// </returns>
    public DeferUsage? GetPrimaryDeferUsage(ConditionFlags deferFlags)
        => GetPrimaryDeferUsage(deferFlags.Word0, deferFlags.Overflow);

    internal DeferUsage? GetPrimaryDeferUsage(ulong deferFlags, ReadOnlySpan<ulong> wideDeferFlags)
    {
        if (_deferUsage.Length == 0)
        {
            return null;
        }

        // Fast path for single defer usage (most common case).
        if (_deferUsage.Length == 1)
        {
            var usage = _deferUsage[0];

            // Walk up the parent chain to find the nearest active defer.
            // A defer directive is inactive when its condition evaluates to false at runtime
            // (e.g. @defer(if: $var) with $var = false). When inactive, the fragment
            // is not deferred and its content folds into the parent scope — but the
            // parent scope may itself be deferred.
            while (usage is not null)
            {
                if (IsConditionBitSet(deferFlags, wideDeferFlags, usage.DeferConditionIndex))
                {
                    return usage;
                }

                usage = usage.Parent;
            }

            // No active defer in the chain — field is not deferred.
            return null;
        }

        // Multiple defer usages: the field was collected from multiple deferred
        // fragments. Resolve each to its nearest active ancestor, then find the
        // outermost (primary) among them.
        DeferUsage? primary = null;

        for (var i = 0; i < _deferUsage.Length; i++)
        {
            // Walk up the parent chain to find the nearest active defer.
            var effective = _deferUsage[i];

            while (effective is not null)
            {
                if (IsConditionBitSet(deferFlags, wideDeferFlags, effective.DeferConditionIndex))
                {
                    break;
                }

                effective = effective.Parent;
            }

            if (effective is null)
            {
                // This occurrence has no active defer in its chain —
                // the field appears non-deferred and belongs in the initial response.
                return null;
            }

            if (primary is null || primary == effective)
            {
                primary = effective;
                continue;
            }

            // Two different active defers. Keep the outermost: check if
            // effective is an ancestor of primary.
            var ancestor = primary.Parent;

            while (ancestor is not null)
            {
                if (ancestor == effective)
                {
                    primary = effective;
                    break;
                }

                ancestor = ancestor.Parent;
            }
        }

        return primary;
    }

    /// <summary>
    /// Returns all active defer usages for this selection given the active defer flags.
    /// If any occurrence of the field is non-deferred (i.e., a defer usage chain leads to no
    /// active defer), returns <c>null</c> — the field belongs in the initial response.
    /// Parent-child pruning is applied: if a parent and child defer are both active,
    /// only the parent is kept.
    /// </summary>
    /// <param name="deferFlags">The active defer flags.</param>
    /// <returns>
    /// The array of active defer usages (pruned), or <c>null</c> if the field is not deferred.
    /// </returns>
    [Obsolete("Use GetActiveDeferUsages(ConditionFlags) instead. This overload throws for operations with more than 64 conditions.")]
    public DeferUsage[]? GetActiveDeferUsages(ulong deferFlags)
        => GetActiveDeferUsagesNarrow(deferFlags);

    internal DeferUsage[]? GetActiveDeferUsagesNarrow(ulong deferFlags)
    {
        if ((_flags & Flags.RequiresWideDeferFlags) != 0)
        {
            throw new InvalidOperationException(
                "The operation has more than 64 defer conditions; this check requires "
                + "the wide defer flags. Use GetActiveDeferUsages(ConditionFlags).");
        }

        return GetActiveDeferUsages(deferFlags, default);
    }

    /// <summary>
    /// Returns all active defer usages for this selection, evaluating all words of the
    /// request defer flags.
    /// </summary>
    /// <param name="deferFlags">The defer condition flags for condition indexes 0-63.</param>
    /// <returns>
    /// The array of active defer usages (pruned), or <c>null</c> if the field is not deferred.
    /// </returns>
    public DeferUsage[]? GetActiveDeferUsages(ConditionFlags deferFlags)
        => GetActiveDeferUsages(deferFlags.Word0, deferFlags.Overflow);

    internal DeferUsage[]? GetActiveDeferUsages(ulong deferFlags, ReadOnlySpan<ulong> wideDeferFlags)
    {
        if (_deferUsage.Length == 0)
        {
            return null;
        }

        // Fast path for single defer usage (most common case).
        if (_deferUsage.Length == 1)
        {
            var usage = _deferUsage[0];

            while (usage is not null)
            {
                if (IsConditionBitSet(deferFlags, wideDeferFlags, usage.DeferConditionIndex))
                {
                    return [usage];
                }

                usage = usage.Parent;
            }

            return null;
        }

        // Multiple defer usages: resolve each to its nearest active ancestor.
        DeferUsage[]? result = null;
        var count = 0;

        for (var i = 0; i < _deferUsage.Length; i++)
        {
            var effective = _deferUsage[i];

            while (effective is not null)
            {
                if (IsConditionBitSet(deferFlags, wideDeferFlags, effective.DeferConditionIndex))
                {
                    break;
                }

                effective = effective.Parent;
            }

            if (effective is null)
            {
                // This occurrence has no active defer in its chain.
                // The field appears non-deferred and belongs in the initial response.
                return null;
            }

            // Check if we already have this effective usage (dedup).
            var isDuplicate = false;
            if (result is not null)
            {
                for (var j = 0; j < count; j++)
                {
                    if (result[j] == effective)
                    {
                        isDuplicate = true;
                        break;
                    }
                }
            }

            if (!isDuplicate)
            {
                result ??= new DeferUsage[_deferUsage.Length];
                result[count++] = effective;
            }
        }

        if (result is null || count == 0)
        {
            return null;
        }

        // Prune parent-child: if a parent and child are both in the set,
        // remove the child (keep only the outermost).
        for (var i = count - 1; i >= 0; i--)
        {
            var ancestor = result[i].Parent;

            while (ancestor is not null)
            {
                for (var j = 0; j < count; j++)
                {
                    if (j != i && result[j] == ancestor)
                    {
                        // result[i] is a child of result[j] — remove it
                        // and break out of both the inner for and while loops.
                        result[i] = result[--count];
                        goto nextItem;
                    }
                }

                ancestor = ancestor.Parent;
            }

// We use goto to avoid an additional boolean condition check on every
// while-loop iteration that a break+flag approach would require.
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
    /// Determines whether the specified <paramref name="target"/> defer usage is among
    /// this selection's active defer usages (after resolving inactive defers to their
    /// nearest active ancestor and applying parent-child pruning).
    /// </summary>
    /// <param name="deferFlags">The active defer flags.</param>
    /// <param name="target">The defer usage to look for.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="target"/> is in the active defer usage set.
    /// </returns>
    [Obsolete("Use HasActiveDeferUsage(ConditionFlags, DeferUsage) instead. This overload throws for operations with more than 64 conditions.")]
    public bool HasActiveDeferUsage(ulong deferFlags, DeferUsage target)
        => HasActiveDeferUsageNarrow(deferFlags, target);

    internal bool HasActiveDeferUsageNarrow(ulong deferFlags, DeferUsage target)
    {
        if ((_flags & Flags.RequiresWideDeferFlags) != 0)
        {
            throw new InvalidOperationException(
                "The operation has more than 64 defer conditions; this check requires "
                + "the wide defer flags. Use HasActiveDeferUsage(ConditionFlags, DeferUsage).");
        }

        return HasActiveDeferUsage(deferFlags, default, target);
    }

    /// <summary>
    /// Determines whether the specified <paramref name="target"/> defer usage is among
    /// this selection's active defer usages, evaluating all words of the request defer flags.
    /// </summary>
    /// <param name="deferFlags">The defer condition flags for condition indexes 0-63.</param>
    /// <param name="target">The defer usage to look for.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="target"/> is in the active defer usage set.
    /// </returns>
    public bool HasActiveDeferUsage(ConditionFlags deferFlags, DeferUsage target)
        => HasActiveDeferUsage(deferFlags.Word0, deferFlags.Overflow, target);

    internal bool HasActiveDeferUsage(ulong deferFlags, ReadOnlySpan<ulong> wideDeferFlags, DeferUsage target)
    {
        if (_deferUsage.Length == 0)
        {
            return false;
        }

        // Resolve each defer usage to its nearest active ancestor and check
        // if any resolves to the target. We also need to check that no
        // occurrence is non-deferred (which would make the whole field non-deferred).
        var hasNonDeferred = false;
        var found = false;

        for (var i = 0; i < _deferUsage.Length; i++)
        {
            var effective = _deferUsage[i];

            while (effective is not null)
            {
                if (IsConditionBitSet(deferFlags, wideDeferFlags, effective.DeferConditionIndex))
                {
                    break;
                }

                effective = effective.Parent;
            }

            if (effective is null)
            {
                hasNonDeferred = true;
                break;
            }

            if (effective == target)
            {
                found = true;
            }
        }

        // If any occurrence is non-deferred, the field is not deferred at all.
        if (hasNonDeferred)
        {
            return false;
        }

        return found;
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

    public Selection WithField(ObjectField field)
    {
        ArgumentNullException.ThrowIfNull(field);

        var selection = new Selection(
            Id,
            ResponseName,
            _utf8ResponseName,
            FieldSelectionPath,
            field,
            field.Type,
            _syntaxNodes,
            _includeFlags,
            _wideIncludeFlags,
            _wideIncludeFlagsStride,
            _deferUsage,
            _deferMask,
            _wideDeferMask,
            _flags,
            Arguments,
            Strategy,
            ResolverPipeline,
            PureResolver,
            BatchResolverPipeline);

        selection._declaringSelectionSet = _declaringSelectionSet;

        return selection;
    }

    public Selection WithType(IType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var selection = new Selection(
            Id,
            ResponseName,
            _utf8ResponseName,
            FieldSelectionPath,
            Field,
            type,
            _syntaxNodes,
            _includeFlags,
            _wideIncludeFlags,
            _wideIncludeFlagsStride,
            _deferUsage,
            _deferMask,
            _wideDeferMask,
            _flags,
            Arguments,
            Strategy,
            ResolverPipeline,
            PureResolver,
            BatchResolverPipeline);

        selection._declaringSelectionSet = _declaringSelectionSet;

        return selection;
    }

    public override string ToString()
    {
        if (SyntaxNodes[0].Node.Alias is not null)
        {
            return $"{ResponseName} : {Field.Name}";
        }

        return Field.Name;
    }

    internal void SetResolvers(
        FieldDelegate? resolverPipeline = null,
        PureFieldDelegate? pureResolver = null,
        BatchFieldDelegate? batchResolverPipeline = null)
    {
        if ((_flags & Flags.Sealed) == Flags.Sealed)
        {
            throw new NotSupportedException(Resources.PreparedSelection_ReadOnly);
        }

        ResolverPipeline = resolverPipeline;
        PureResolver = pureResolver;
        BatchResolverPipeline = batchResolverPipeline;
        Strategy = InferStrategy(
            hasPureResolver: pureResolver is not null,
            hasBatchResolver: batchResolverPipeline is not null);
    }

    /// <summary>
    /// Completes the selection without sealing it.
    /// </summary>
    internal void Complete(SelectionSet selectionSet)
    {
        ArgumentNullException.ThrowIfNull(selectionSet);

        if ((_flags & Flags.Sealed) == Flags.Sealed)
        {
            throw new InvalidOperationException("Selection is already sealed.");
        }

        _declaringSelectionSet = selectionSet;

        // Conditional (resp. deferrable) selections of a wide operation must be
        // evaluated with the wide flag overloads; the narrow checks throw for them.
        var operation = selectionSet.DeclaringOperation;

        if (operation.HasWideIncludeFlags && _includeFlags.Length > 0)
        {
            _flags |= Flags.RequiresWideIncludeFlags;
        }

        if (operation.HasWideDeferFlags && _deferUsage.Length > 0)
        {
            _flags |= Flags.RequiresWideDeferFlags;
        }

        _flags |= Flags.Sealed;
    }

    private SelectionExecutionStrategy InferStrategy(
        bool isSerial = false,
        bool hasPureResolver = false,
        bool hasBatchResolver = false)
    {
        // batch resolver takes precedence — it handles its own execution strategy.
        if (hasBatchResolver)
        {
            return SelectionExecutionStrategy.Batch;
        }

        // once a field is marked serial it even with a pure resolver cannot become pure.
        if (Strategy is SelectionExecutionStrategy.Serial || isSerial)
        {
            return SelectionExecutionStrategy.Serial;
        }

        if (hasPureResolver)
        {
            return SelectionExecutionStrategy.Pure;
        }

        return SelectionExecutionStrategy.Default;
    }

    [Flags]
    private enum Flags
    {
        None = 0,
        Internal = 1,
        Sealed = 2,
        List = 4,
        Stream = 8,
        Leaf = 16,
        ProjectionRequirement = 32,
        RequiresWideIncludeFlags = 64,
        RequiresWideDeferFlags = 128
    }
}
