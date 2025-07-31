using System.Collections.Concurrent;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class Operation
{
#if NET9_0_OR_GREATER
    private readonly Lock _sync = new();
#else
    private readonly object _sync = new();
#endif
    private readonly ConcurrentDictionary<(ulong, string), SelectionSet> _selectionSets = [];
    private readonly OperationCompiler _compiler;
    private readonly IncludeConditionCollection _includeConditions;
    private uint _lastId;

    internal Operation(
        string id,
        string hash,
        OperationDefinitionNode definition,
        IObjectTypeDefinition rootType,
        ISchemaDefinition schema,
        SelectionSet rootSelectionSet,
        OperationCompiler compiler,
        IncludeConditionCollection includeConditions,
        uint lastId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(rootType);
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(rootSelectionSet);
        ArgumentNullException.ThrowIfNull(includeConditions);

        Id = id;
        Hash = hash;
        Definition = definition;
        RootType = rootType;
        Schema = schema;
        RootSelectionSet = rootSelectionSet;
        _compiler = compiler;
        _includeConditions = includeConditions;
        _lastId = lastId;

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
                            typeContext,
                            _includeConditions,
                            ref _lastId);
                    selectionSet.Seal(this);
                }
            }
        }

        return selectionSet;
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
}
