using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class Operation : IOperation
{
#if NET9_0_OR_GREATER
    private readonly Lock _sync = new();
#else
    private readonly object _sync = new();
#endif
    private readonly ConcurrentDictionary<(int, string), SelectionSet> _selectionSets = [];
    private readonly OperationCompiler _compiler;
    private readonly IncludeConditionCollection _includeConditions;
    private readonly OperationFeatureCollection _features;
    private object[] _elementsById;
    private int _lastId;

    internal Operation(
        string id,
        string hash,
        OperationDefinitionNode definition,
        IObjectTypeDefinition rootType,
        ISchemaDefinition schema,
        SelectionSet rootSelectionSet,
        OperationCompiler compiler,
        IncludeConditionCollection includeConditions,
        int lastId,
        object[] elementsById)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(rootType);
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(rootSelectionSet);
        ArgumentNullException.ThrowIfNull(compiler);
        ArgumentNullException.ThrowIfNull(includeConditions);
        ArgumentNullException.ThrowIfNull(elementsById);

        Id = id;
        Hash = hash;
        Definition = definition;
        RootType = rootType;
        Schema = schema;
        RootSelectionSet = rootSelectionSet;
        _compiler = compiler;
        _includeConditions = includeConditions;
        _lastId = lastId;
        _elementsById = elementsById;

        _features = new OperationFeatureCollection();
        rootSelectionSet.Seal(this);
    }

    /// <summary>
    /// Gets the internal unique identifier for this operation.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the hash of the original operation document.
    /// </summary>
    public string Hash { get; }

    /// <summary>
    /// Gets the name of the operation.
    /// </summary>
    public string? Name => Definition.Name?.Value;

    /// <summary>
    /// Gets the syntax node representing the operation definition.
    /// </summary>
    public OperationDefinitionNode Definition { get; }

    /// <summary>
    /// Gets the root type on which the operation is executed.
    /// </summary>
    public IObjectTypeDefinition RootType { get; }

    /// <summary>
    /// Gets the schema for which this operation is compiled.
    /// </summary>
    public ISchemaDefinition Schema { get; }

    /// <summary>
    /// Gets the prepared root selections for this operation.
    /// </summary>
    /// <returns>
    /// Returns the prepared root selections for this operation.
    /// </returns>
    public SelectionSet RootSelectionSet { get; }

    ISelectionSet IOperation.RootSelectionSet
        => RootSelectionSet;

    /// <inheritdoc cref="IFeatureProvider"/>
    public IFeatureCollection Features => _features;

    public bool HasIncrementalParts => throw new NotImplementedException();

    /// <summary>
    /// Gets the selection set for the specified <paramref name="selection"/>
    /// if the selections named return type is an object type.
    /// </summary>
    /// <param name="selection">
    /// The selection set for which the selection set shall be resolved.
    /// </param>
    /// <returns>
    /// Returns the selection set for the specified <paramref name="selection"/> and
    /// the named return type of the selection.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// - The specified <paramref name="selection"/> has no selection set.
    /// - The specified <paramref name="selection"/> returns an abstract named type.
    /// </exception>
    public SelectionSet GetSelectionSet(Selection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        var typeContext = selection.Field.Type.NamedType<IObjectTypeDefinition>();
        return GetSelectionSet(selection, typeContext);
    }

    /// <summary>
    /// Gets the selection set for the specified <paramref name="selection"/> and
    /// <paramref name="typeContext"/>.
    /// </summary>
    /// <param name="selection">
    /// The selection set for which the selection set shall be resolved.
    /// </param>
    /// <param name="typeContext">
    /// The result type context.
    /// </param>
    /// <returns>
    /// Returns the selection set for the specified <paramref name="selection"/> and
    /// <paramref name="typeContext"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The specified <paramref name="selection"/> has no selection set.
    /// </exception>
    public SelectionSet GetSelectionSet(Selection selection, IObjectTypeDefinition typeContext)
    {
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(typeContext);
        Debug.Assert(typeContext is FusionObjectTypeDefinition);

        var key = (selection.Id, typeContext.Name);

        if (!_selectionSets.TryGetValue(key, out var selectionSet))
        {
            lock (_sync)
            {
                if (!_selectionSets.TryGetValue(key, out selectionSet))
                {
                    selectionSet =
                        _compiler.CompileSelectionSet(
                            selection,
                            (FusionObjectTypeDefinition)typeContext,
                            _includeConditions,
                            ref _elementsById,
                            ref _lastId);
                    selectionSet.Seal(this);
                    _selectionSets.TryAdd(key, selectionSet);
                }
            }
        }

        return selectionSet;
    }

    ISelectionSet IOperation.GetSelectionSet(ISelection selection, IObjectTypeDefinition typeContext)
    {
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(typeContext);

        if (selection is not Selection internalSelection)
        {
            throw new InvalidOperationException(
                $"Only selections of the type {typeof(Selection).FullName} are supported.");
        }

        return GetSelectionSet(internalSelection, typeContext);
    }

    /// <summary>
    /// Gets the possible return types for the <paramref name="selection"/>.
    /// </summary>
    /// <param name="selection">
    /// The selection for which the possible result types shall be returned.
    /// </param>
    /// <returns>
    /// Returns the possible return types for the specified <paramref name="selection"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The specified <paramref name="selection"/> has no selection set.
    /// </exception>
    public IEnumerable<IObjectTypeDefinition> GetPossibleTypes(Selection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        return Schema.GetPossibleTypes(selection.Field.Type.NamedType());
    }

    IEnumerable<IObjectTypeDefinition> IOperation.GetPossibleTypes(ISelection selection)
        => Schema.GetPossibleTypes(selection.Field.Type.NamedType());

    /// <summary>
    /// Creates the include flags for the specified variable values.
    /// </summary>
    /// <param name="variables">
    /// The variable values.
    /// </param>
    /// <returns>
    /// Returns the include flags for the specified variable values.
    /// </returns>
    public ulong CreateIncludeFlags(IVariableValueCollection variables)
    {
        var index = 0;
        var includeFlags = 0ul;

        foreach (var includeCondition in _includeConditions)
        {
            if (includeCondition.IsIncluded(variables))
            {
                includeFlags |= 1ul << index++;
            }
        }

        return includeFlags;
    }

    internal Selection GetSelectionById(int id)
        => Unsafe.As<Selection>(Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_elementsById), id));

    internal SelectionSet GetSelectionSetById(int id)
        => Unsafe.As<SelectionSet>(Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_elementsById), id));
}
