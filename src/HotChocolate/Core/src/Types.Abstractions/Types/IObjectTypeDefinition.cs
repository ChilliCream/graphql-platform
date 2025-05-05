using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// <para>
/// GraphQL operations are hierarchical and composed, describing a tree of information.
/// While Scalar types describe the leaf values of these hierarchical operations,
/// Objects describe the intermediate levels.
/// </para>
/// <para>
/// GraphQL Objects represent a list of named fields, each of which yield a value of a
/// specific type. Object values should be serialized as ordered maps, where the selected
/// field names (or aliases) are the keys and the result of evaluating the field is the value,
/// ordered by the order in which they appear in the selection set.
/// </para>
/// <para>
/// All fields defined within an Object type must not have a name which begins
/// with "__" (two underscores), as this is used exclusively by
/// GraphQL’s introspection system.
/// </para>
/// </summary>
public interface IObjectTypeDefinition : IComplexTypeDefinition
{
    /// <summary>
    /// Creates an <see cref="ObjectTypeDefinitionNode"/> from the current <see cref="IObjectTypeDefinition"/>.
    /// </summary>
    /// <returns>
    /// Returns an <see cref="ObjectTypeDefinitionNode"/>.
    /// </returns>
    new ObjectTypeDefinitionNode ToSyntaxNode();
}
