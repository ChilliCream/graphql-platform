using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution;

public interface IOperation : IFeatureProvider
{
    /// <summary>
    /// Gets the internal unique identifier for this operation.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the hash of the original operation document.
    /// </summary>
    string Hash { get; }

    /// <summary>
    /// Gets the name of the operation.
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// Gets the syntax node representing the operation definition.
    /// </summary>
    OperationDefinitionNode Definition { get; }

    /// <summary>
    /// Gets the root type on which the operation is executed.
    /// </summary>
    IObjectTypeDefinition RootType { get; }

    /// <summary>
    /// Gets the schema for which this operation is compiled.
    /// </summary>
    ISchemaDefinition Schema { get; }

    /// <summary>
    /// Gets the prepared root selections for this operation.
    /// </summary>
    /// <returns>
    /// Returns the prepared root selections for this operation.
    /// </returns>
    ISelectionSet RootSelectionSet { get; }

    /// <summary>
    /// Gets a value indicating whether this operation contains incremental delivery directives
    /// such as <c>@defer</c> or <c>@stream</c>.
    /// </summary>
    /// <value>
    /// <c>true</c> if the operation contains <c>@defer</c> or <c>@stream</c> directives;
    /// otherwise, <c>false</c>.
    /// </value>
    bool HasIncrementalParts { get; }

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
    ISelectionSet GetSelectionSet(ISelection selection, IObjectTypeDefinition typeContext);

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
    IEnumerable<IObjectTypeDefinition> GetPossibleTypes(ISelection selection);
}
