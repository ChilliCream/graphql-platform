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
    private readonly byte[] _utf8ResponseName;
    private Flags _flags;
    private SelectionSet? _declaringSelectionSet;

    internal Selection(
        int id,
        string responseName,
        ObjectField field,
        FieldSelectionNode[] syntaxNodes,
        ulong[] includeFlags,
        bool isInternal = false,
        ArgumentMap? arguments = null,
        FieldDelegate? resolverPipeline = null,
        PureFieldDelegate? pureResolver = null)
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
        Type = field.Type;
        Arguments = arguments ?? s_emptyArguments;
        ResolverPipeline = resolverPipeline;
        PureResolver = pureResolver;
        Strategy = InferStrategy(
            isSerial: !field.IsParallelExecutable,
            hasPureResolver: pureResolver is not null);
        _syntaxNodes = syntaxNodes;
        _includeFlags = includeFlags;
        _flags = isInternal ? Flags.Internal : Flags.None;

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
        ObjectField field,
        IType type,
        FieldSelectionNode[] syntaxNodes,
        ulong[] includeFlags,
        Flags flags,
        ArgumentMap? arguments,
        SelectionExecutionStrategy strategy,
        FieldDelegate? resolverPipeline,
        PureFieldDelegate? pureResolver)
    {
        Id = id;
        ResponseName = responseName;
        Field = field;
        Type = type;
        Arguments = arguments ?? s_emptyArguments;
        ResolverPipeline = resolverPipeline;
        PureResolver = pureResolver;
        Strategy = strategy;
        _syntaxNodes = syntaxNodes;
        _includeFlags = includeFlags;
        _flags = flags;
        _utf8ResponseName = utf8ResponseName;
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
    public bool IsSkipped(ulong includeFlags)
        => !IsIncluded(includeFlags);

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

    public Selection WithField(ObjectField field)
    {
        ArgumentNullException.ThrowIfNull(field);

        var selection = new Selection(
            Id,
            ResponseName,
            _utf8ResponseName,
            field,
            field.Type,
            _syntaxNodes,
            _includeFlags,
            _flags,
            Arguments,
            Strategy,
            ResolverPipeline,
            PureResolver);

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
            Field,
            type,
            _syntaxNodes,
            _includeFlags,
            _flags,
            Arguments,
            Strategy,
            ResolverPipeline,
            PureResolver);

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
        PureFieldDelegate? pureResolver = null)
    {
        if ((_flags & Flags.Sealed) == Flags.Sealed)
        {
            throw new NotSupportedException(Resources.PreparedSelection_ReadOnly);
        }

        ResolverPipeline = resolverPipeline;
        PureResolver = pureResolver;
        Strategy = InferStrategy(hasPureResolver: pureResolver is not null);
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
        _flags |= Flags.Sealed;
    }

    private SelectionExecutionStrategy InferStrategy(
        bool isSerial = false,
        bool hasPureResolver = false)
    {
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
        Leaf = 16
    }
}
