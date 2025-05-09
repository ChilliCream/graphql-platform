using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// <para>
/// GraphQL Unions represent an object that could be one of a list of GraphQL Object types,
/// but provides for no guaranteed fields between those types.
/// </para>
/// <para>
/// They also differ from interfaces in that Object types declare what interfaces
/// they implement, but are not aware of what unions contain them.
/// </para>
/// <para>
/// With interfaces and objects, only those fields defined on the type can be queried directly;
/// to query other fields on an interface, typed fragments must be used.
/// This is the same as for unions, but unions do not define any fields,
/// so no fields may be queried on this type without the use of type refining
/// fragments or inline fragments (with the exception of the meta-field __typename).
/// </para>
/// <para>
/// For example, we might define the following types:
///</para>
/// <code>
/// union SearchResult = Photo | Person
///
/// type Person {
///   name: String
///   age: Int
/// }
///
/// type Photo {
///   height: Int
///   width: Int
/// }
///
/// type SearchQuery {
///   firstSearchResult: SearchResult
/// }
/// </code>
/// </summary>
public interface IUnionTypeDefinition
    : IOutputTypeDefinition
    , ISyntaxNodeProvider<UnionTypeDefinitionNode>
    , ISchemaCoordinateProvider
{
    /// <summary>
    /// Gets the <see cref="IObjectTypeDefinition" /> set of this union type.
    /// </summary>
    IReadOnlyObjectTypeDefinitionCollection Types { get; }
}
