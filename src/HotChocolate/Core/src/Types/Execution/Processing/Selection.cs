using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using HotChocolate.Caching.Memory;
using HotChocolate.Execution.Properties;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// Represents a field selection during execution.
/// </summary>
public class Selection : ISelection
{
    private static readonly ArgumentMap s_emptyArguments = ArgumentMap.Empty;
    private readonly FieldSelectionNode[] _syntaxNodes;
    private readonly ulong[] _includeFlags;
    private readonly byte[] _utf8ResponseName;
    private Flags _flags;

    internal Selection(
        int id,
        string responseName,
        ObjectField field,
        FieldSelectionNode[] syntaxNodes,
        ulong[] includeFlags,
        bool isInternal,
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

    /// <inheritdoc />
    public int Id { get; }

    /// <inheritdoc />
    public string ResponseName { get; }

    internal ReadOnlySpan<byte> Utf8ResponseName => _utf8ResponseName;

    public bool IsInternal => (_flags & Flags.Internal) == Flags.Internal;

    public bool IsConditional => _includeFlags.Length > 0;

    public bool IsList => (_flags & Flags.List) == Flags.List;

    public bool IsLeaf => (_flags & Flags.Leaf) == Flags.Leaf;

    /// <summary>
    /// Defines if this selection has child selections.
    /// </summary>
    public bool HasSelections => !IsLeaf;

    public ObjectField Field { get; }

    /// <inheritdoc />
    IOutputFieldDefinition ISelection.Field => Field;

    public IType Type => Field.Type;

    public ObjectType DeclaringType => Field.DeclaringType;

    public SelectionSet DeclaringSelectionSet { get; private set; } = null!;

    ISelectionSet ISelection.DeclaringSelectionSet => DeclaringSelectionSet;

    public Operation DeclaringOperation => DeclaringSelectionSet.DeclaringOperation;

    public ArgumentMap Arguments { get; }

    public SelectionExecutionStrategy Strategy { get; private set; }

    public FieldDelegate? ResolverPipeline { get; private set; }

    public PureFieldDelegate? PureResolver { get; private set; }

    public ReadOnlySpan<FieldSelectionNode> SyntaxNodes => _syntaxNodes;

    IEnumerable<FieldNode> ISelection.GetSyntaxNodes()
    {
        for (var i = 0; i < SyntaxNodes.Length; i++)
        {
            yield return SyntaxNodes[i].Node;
        }
    }

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
    internal void Complete(SelectionSet selectionSet, bool seal)
    {
        ArgumentNullException.ThrowIfNull(selectionSet);

        if ((_flags & Flags.Sealed) == Flags.Sealed)
        {
            throw new InvalidOperationException("Selection is already sealed.");
        }

        DeclaringSelectionSet = selectionSet;

        if (seal)
        {
            _flags |= Flags.Sealed;
        }
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

    internal sealed class Sealed : Selection
    {
        public Sealed(
            int id,
            ObjectType declaringType,
            ObjectField field,
            IType type,
            FieldNode syntaxNode,
            string responseName,
            ArgumentMap? arguments = null,
            ulong[]? includeConditions = null,
            bool isInternal = false,
            bool isParallelExecutable = true,
            FieldDelegate? resolverPipeline = null,
            PureFieldDelegate? pureResolver = null) : base(
            id,
            declaringType,
            field,
            type,
            syntaxNode,
            responseName,
            arguments,
            includeConditions,
            isInternal,
            isParallelExecutable,
            resolverPipeline,
            pureResolver)
        {
        }
    }
}

internal static class Utf8StringCache
{
    private static readonly Encoding s_utf8 = Encoding.UTF8;
    private static readonly Cache<byte[]> s_cache = new(capacity: 4 * 1024);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] GetUtf8String(string s)
        => s_cache.GetOrCreate(s, static k => s_utf8.GetBytes(k));
}
