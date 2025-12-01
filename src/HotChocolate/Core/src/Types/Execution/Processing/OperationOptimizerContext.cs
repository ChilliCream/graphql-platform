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
    private readonly CreateFieldPipeline _createFieldPipeline;

    /// <summary>
    /// Initializes a new instance of <see cref="OperationOptimizerContext"/>
    /// </summary>
    internal OperationOptimizerContext(
        Operation operation,
        Dictionary<string, object?> contextData,
        CreateFieldPipeline createFieldPipeline)
    {
        Operation = operation;
        ContextData = contextData;
        _createFieldPipeline = createFieldPipeline;
    }

    /// <summary>
    /// Gets the operation.
    /// </summary>
    public Operation Operation { get; }

    /// <summary>
    /// The context data dictionary can be used by middleware components and
    /// resolvers to store and retrieve data during execution.
    /// </summary>
    public IDictionary<string, object?> ContextData { get; }

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
        Selection selection,
        FieldDelegate? resolverPipeline = null,
        PureFieldDelegate? pureResolver = null)
        => selection.SetResolvers(resolverPipeline, pureResolver);

    /// <summary>
    /// Allows to compile the field resolver pipeline for a field.
    /// </summary>
    /// <param name="field">The field.</param>
    /// <param name="fieldSelection">The selection of the field.</param>
    /// <returns>
    /// Returns a <see cref="FieldDelegate" /> representing the field resolver pipeline.
    /// </returns>
    public FieldDelegate CompileResolverPipeline(
        ObjectField field,
        FieldNode fieldSelection)
        => _createFieldPipeline(Operation.Schema, field, fieldSelection);
}
