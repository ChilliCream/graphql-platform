using System.Collections.Generic;
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
    private readonly IReadOnlyList<SelectionVariants> _variants;

    /// <summary>
    /// Initializes a new instance of <see cref="OperationOptimizerContext"/>
    /// </summary>
    internal OperationOptimizerContext(
        ISchema schema,
        IObjectType rootType,
        SelectionVariants[] variants,
        Dictionary<string, object?> contextData)
    {
        _variants = variants;
        Schema = schema;
        RootType = rootType;
        ContextData = contextData;
    }


    /// <summary>
    /// Gets the schema for which the query is compiled.
    /// </summary>
    public ISchema Schema { get; }

    /// <summary>
    /// Gets the root type on which the operation is executed.
    /// </summary>
    public IObjectType RootType { get; }

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
        => OperationCompiler.CreateFieldMiddleware(Schema, field, selection);
}
