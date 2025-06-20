using System.ComponentModel;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

public sealed class Operation
{
    private readonly IncludeConditionCollection _includeConditions;
    private readonly ulong _lastId;

    internal Operation(
        string id,
        DocumentNode document,
        OperationDefinitionNode definition,
        IObjectTypeDefinition rootType,
        ISchemaDefinition schema,
        SelectionSet rootSelectionSet,
        IncludeConditionCollection includeConditions,
        ulong lastId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(rootType);
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(rootSelectionSet);
        ArgumentNullException.ThrowIfNull(includeConditions);

        Id = id;
        Document = document;
        Definition = definition;
        RootType = rootType;
        Schema = schema;
        RootSelectionSet = rootSelectionSet;
        _includeConditions = includeConditions;
        _lastId = lastId;
    }

    /// <summary>
    /// Gets the internal unique identifier for this operation.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the name of the operation.
    /// </summary>
    public string? Name => Definition.Name?.Value;

    /// <summary>
    /// Gets the parsed query document that contains the
    /// operation-<see cref="Definition" />.
    /// </summary>
    public DocumentNode Document { get; }

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
        throw new NotImplementedException();
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
    public long CreateIncludeFlags(IVariableValueCollection variables)
    {
        throw new NotImplementedException();
    }
}
