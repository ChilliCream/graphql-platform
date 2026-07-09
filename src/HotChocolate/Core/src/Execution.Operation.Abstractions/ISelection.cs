using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution;

/// <summary>
/// Represents a single field selection within a GraphQL operation's execution plan.
/// This is the result of compiling and merging field selections from the original
/// GraphQL document, including resolution of fragments.
/// </summary>
/// <remarks>
/// Multiple field nodes from the original document may be represented by a single
/// ISelection if they have the same response name and are eligible for field merging.
/// For example, multiple selections of the same field with different arguments
/// would still result in a single ISelection that represents the merged selection.
/// </remarks>
public interface ISelection
{
    /// <summary>
    /// Gets the unique identifier for this selection within the operation.
    /// </summary>
    /// <value>
    /// A numeric identifier that uniquely identifies this selection within
    /// its declaring operation. This ID is stable and can be used for caching
    /// and optimization purposes.
    /// </value>
    int Id { get; }

    /// <summary>
    /// Gets the response name for this selection.
    /// </summary>
    /// <value>
    /// The name that will be used as the key in the response object. This is
    /// either the field name or the alias if one was specified. For example,
    /// in <c>userName: user { name }</c>, the response name would be "userName".
    /// </value>
    string ResponseName { get; }

    /// <summary>
    /// Gets the selection set that contains this selection.
    /// </summary>
    /// <value>
    /// The parent selection set that declares this selection. This provides
    /// context about where this selection appears in the operation structure.
    /// </value>
    ISelectionSet DeclaringSelectionSet { get; }

    /// <summary>
    /// Gets the field definition from the schema that this selection targets.
    /// </summary>
    /// <value>
    /// The schema field definition that provides type information, resolver
    /// details, and other metadata needed for field execution.
    /// </value>
    IOutputFieldDefinition Field { get; }

    /// <summary>
    /// Gets the GraphQL type that this selection will return.
    /// </summary>
    /// <value>
    /// The output type of this field selection, including nullability and
    /// list wrapper information. This type information is used during
    /// execution for validation and coercion.
    /// </value>
    IType Type { get; }

    /// <summary>
    /// Gets a value indicating whether this selection is conditional.
    /// </summary>
    /// <value>
    /// <c>true</c> if this selection has @skip or @include directives that
    /// make its inclusion dependent on variable values; otherwise, <c>false</c>.
    /// Conditional selections require include flag evaluation during execution.
    /// </value>
    bool IsConditional { get; }

    /// <summary>
    /// Gets a value indicating whether this selection is for internal use.
    /// </summary>
    /// <value>
    /// <c>true</c> if this selection was added by the execution engine for
    /// internal purposes (e.g., introspection fields, type name resolution);
    /// otherwise, <c>false</c> for user-requested selections.
    /// </value>
    bool IsInternal { get; }

    /// <summary>
    /// Gets a value indicating whether this selection is a leaf node.
    /// </summary>
    /// <value>
    /// <c>true</c> if this selection is a leaf node; otherwise, <c>false</c>.
    /// </value>
    bool IsLeaf { get; }

    /// <summary>
    /// Gets the original syntax nodes that contributed to this selection.
    /// </summary>
    /// <returns>
    /// An enumerable collection of field nodes from the original GraphQL document that
    /// were merged to create this selection. Multiple nodes may be returned
    /// if field merging occurred (same response name, compatible arguments).
    /// </returns>
    /// <remarks>
    /// This method provides access to the original AST nodes for advanced
    /// scenarios like custom directive processing or detailed error reporting.
    /// Most execution logic should not need to access the syntax nodes directly.
    /// </remarks>
    IEnumerable<FieldNode> GetSyntaxNodes();

    /// <summary>
    /// Determines whether this selection should be included based on conditional flags.
    /// </summary>
    /// <param name="includeFlags">
    /// The conditional inclusion flags.
    /// </param>
    /// <returns>
    /// <c>true</c> if this selection should be executed given the current
    /// variable values; otherwise, <c>false</c> if it should be skipped
    /// due to @skip or @include directive evaluation.
    /// </returns>
    /// <remarks>
    /// For non-conditional selections, this always returns <c>true</c>.
    /// </remarks>
    bool IsIncluded(ulong includeFlags);

    /// <summary>
    /// Determines whether this selection is deferred based on the <c>@defer</c> directive flags.
    /// </summary>
    /// <param name="deferFlags">
    /// The defer condition flags representing which <c>@defer</c> directives are active
    /// for the current request, computed from the runtime variable values of the
    /// <c>if</c> arguments on <c>@defer</c> directives.
    /// </param>
    /// <returns>
    /// <c>true</c> if this selection should be deferred and delivered incrementally
    /// in a subsequent payload; otherwise, <c>false</c> if it should be included
    /// in the initial response.
    /// </returns>
    /// <remarks>
    /// <para>
    /// For selections without any <c>@defer</c> directive, this always returns <c>false</c>.
    /// </para>
    /// <para>
    /// The <c>@defer</c> directive (as specified in the GraphQL Incremental Delivery
    /// specification) allows clients to mark fragment spreads or inline fragments
    /// as lower priority for the initial response. The server may then choose
    /// to deliver those fragments in subsequent payloads, reducing time-to-first-byte
    /// for the initial result.
    /// </para>
    /// </remarks>
    bool IsDeferred(ulong deferFlags);
}
