using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// The <see cref="Operation"/> optimizer provides helper methods
/// to optimize a <see cref="Operation"/> and store additional execution metadata.
/// </summary>
public readonly ref struct OperationOptimizerContext
{
    private readonly SelectionVariants[] _variants;
    private readonly IncludeCondition[] _includeConditions;
    private readonly ObjectType _rootType;
    private readonly Dictionary<string, object?> _contextData;
    private readonly bool _hasIncrementalParts;
    private readonly CreateFieldPipeline _createFieldPipeline;

    /// <summary>
    /// Initializes a new instance of <see cref="OperationOptimizerContext"/>
    /// </summary>
    internal OperationOptimizerContext(
        string id,
        DocumentNode document,
        OperationDefinitionNode definition,
        ISchema schema,
        ObjectType rootType,
        SelectionVariants[] variants,
        IncludeCondition[] includeConditions,
        Dictionary<string, object?> contextData,
        bool hasIncrementalParts,
        CreateFieldPipeline createFieldPipeline)
    {
        Id = id;
        Document = document;
        Definition = definition;
        Schema = schema;
        _rootType = rootType;
        _variants = variants;
        _includeConditions = includeConditions;
        _contextData = contextData;
        _hasIncrementalParts = hasIncrementalParts;
        _createFieldPipeline = createFieldPipeline;
    }

    /// <summary>
    /// Gets the internal unique identifier for this operation.
    /// </summary>
    public string Id { get; }

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
    /// Gets the schema for which the query is compiled.
    /// </summary>
    public ISchema Schema { get; }

    /// <summary>
    /// Gets the root type on which the operation is executed.
    /// </summary>
    public IObjectType RootType => _rootType;

    /// <summary>
    /// Gets the prepared root selections for this operation.
    /// </summary>
    public ISelectionSet RootSelectionSet => _variants[0].GetSelectionSet(RootType);

    /// <summary>
    /// Gets all selection variants of this operation.
    /// </summary>
    public IReadOnlyList<ISelectionVariants> SelectionVariants => _variants;

    /// <summary>
    /// The context data dictionary can be used by middleware components and
    /// resolvers to store and retrieve data during execution.
    /// </summary>
    public IDictionary<string, object?> ContextData => _contextData;

    /// <summary>
    /// Defines if the operation has incremental parts.
    /// </summary>
    public bool HasIncrementalParts => _hasIncrementalParts;

    /// <summary>
    /// Sets the resolvers on the specified <paramref name="selection"/>.
    /// </summary>
    /// <param name="selection">
    /// The selection to set the resolvers on.
    /// </param>
    /// <param name="resolverPipeline">
    /// The async resolver pipeline.
    /// </param>
    /// <param name="pureResolver">
    /// The pure resolver.
    /// </param>
    public void SetResolver(
        ISelection selection,
        FieldDelegate? resolverPipeline = null,
        PureFieldDelegate? pureResolver = null)
        => ((Selection)selection).SetResolvers(resolverPipeline, pureResolver);

    /// <summary>
    /// Allows to compile the field resolver pipeline for a field.
    /// </summary>
    /// <param name="field">The field.</param>
    /// <param name="selection">The selection of the field.</param>
    /// <returns>
    /// Returns a <see cref="FieldDelegate" /> representing the field resolver pipeline.
    /// </returns>
    public FieldDelegate CompileResolverPipeline(IObjectField field, FieldNode selection)
        => _createFieldPipeline(Schema, field, selection);

    /// <summary>
    /// Creates a temporary operation object for the optimizer.
    /// </summary>
    public IOperation CreateOperation()
    {
        var operation = new Operation(Id, Document, Definition, _rootType, Schema);
        operation.Seal(_contextData, _variants, _hasIncrementalParts, _includeConditions);
        return operation;
    }
}
