#nullable enable

using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// Represents a field selection during execution.
/// </summary>
public interface ISelection : IOptionalSelection
{
    /// <summary>
    /// Gets an operation unique identifier of this selection.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Gets the name this field will have in the response map.
    /// </summary>
    string ResponseName { get; }

    /// <summary>
    /// Gets the field that was selected.
    /// </summary>
    IObjectField Field { get; }

    /// <summary>
    /// Gets the type of the selection.
    /// </summary>
    IType Type { get; }

    /// <summary>
    /// Gets the type kind of the selection.
    /// </summary>
    TypeKind TypeKind { get; }

    /// <summary>
    /// Specifies if the return type of this selection is a list type.
    /// </summary>
    bool IsList { get; }

    /// <summary>
    /// Gets the type that declares the field that is selected by this selection.
    /// </summary>
    IObjectType DeclaringType { get; }

    /// <summary>
    /// Gets the selectionSet that declares this selection.
    /// </summary>
    ISelectionSet DeclaringSelectionSet { get; }

    /// <summary>
    /// Gets the operation that declares this selection.
    /// </summary>
    IOperation DeclaringOperation { get; }

    /// <summary>
    /// Gets the merged field selection syntax node.
    /// </summary>
    FieldNode SyntaxNode { get; }

    /// <summary>
    /// Gets the field selection syntax node.
    /// </summary>
    IReadOnlyList<FieldNode> SyntaxNodes { get; }

    /// <summary>
    /// If this selection selects a field that returns a composite type
    /// then this selection set represents the fields that are selected
    /// on that returning composite type.
    ///
    /// If this selection however selects a leaf field than this
    /// selection set will be <c>null</c>.
    /// </summary>
    SelectionSetNode? SelectionSet { get; }

    /// <summary>
    /// Gets the execution kind.
    /// </summary>
    SelectionExecutionStrategy Strategy { get; }

    /// <summary>
    /// The compiled resolver pipeline for this selection.
    /// </summary>
    FieldDelegate? ResolverPipeline { get; }

    /// <summary>
    /// The compiled pure resolver.
    /// </summary>
    PureFieldDelegate? PureResolver { get; }

    /// <summary>
    /// The arguments that have been pre-coerced for this field selection.
    /// </summary>
    ArgumentMap Arguments { get; }

    /// <summary>
    /// Defines if this selection is annotated with the stream directive.
    /// </summary>
    /// <param name="includeFlags">
    /// The execution include flags that determine if the stream directive is applied for the
    /// current execution run.
    /// </param>
    /// <returns>
    /// Returns if this selection is annotated with the stream directive.
    /// </returns>
    bool HasStreamDirective(long includeFlags);
}
