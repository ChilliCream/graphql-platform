namespace HotChocolate.Types;

/// <summary>
/// Specifies the GraphQL type kind.
/// </summary>
public enum TypeKind
{
    /// <summary>
    /// <para>
    /// GraphQL interfaces represent a list of named fields and their arguments.
    /// GraphQL objects and interfaces can then implement these interfaces
    /// which requires that the implementing type will define all fields defined by those
    /// interfaces.
    /// </para>
    /// <para>
    /// Fields on a GraphQL interface have the same rules as fields on a GraphQL object;
    /// their type can be Scalar, Object, Enum, Interface, or Union, or any wrapping type
    /// whose base type is one of those five.
    /// </para>
    /// <para>
    /// For example, an interface NamedEntity may describe a required field and types such
    /// as Person or Business may then implement this interface to guarantee this field will
    /// always exist.
    /// </para>
    /// <para>
    /// Types may also implement multiple interfaces. For example, Business implements both
    /// the NamedEntity and ValuedEntity interfaces in the example below.
    /// </para>
    ///
    /// <code>
    /// interface NamedEntity {
    ///   name: String
    /// }
    ///
    /// interface ValuedEntity {
    ///   value: Int
    /// }
    ///
    /// type Person implements NamedEntity {
    ///   name: String
    ///   age: Int
    /// }
    ///
    /// type Business implements NamedEntity &amp; ValuedEntity {
    ///   name: String
    ///   value: Int
    ///   employeeCount: Int
    /// }
    /// </code>
    /// </summary>
    Interface = 0,

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
    Object = 1,

    /// <summary>
    /// <para>
    /// GraphQL Unions represent an object that could be one of a list of GraphQL Object types,
    /// but provides for no guaranteed fields between those types.
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
    /// <para>For example, we might define the following types:</para>
    ///
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
    Union = 2,

    /// <summary>
    /// <para>
    /// A GraphQL Input Object defines a set of input fields; the input fields are either scalars,
    /// enums, or other input objects. This allows arguments to accept arbitrarily complex structs.
    /// </para>
    /// <para>In this example, an Input Object called Point2D describes x and y inputs:</para>
    ///
    /// <code>
    /// input Point2D {
    ///   x: Float
    ///   y: Float
    /// }
    /// </code>
    /// </summary>
    InputObject = 4,

    /// <summary>
    /// <para>
    /// GraphQL Enum types, like Scalar types, also represent leaf values in a GraphQL type system.
    /// However Enum types describe the set of possible values.
    /// </para>
    /// <para>
    /// Enums are not references for a numeric value, but are unique values in their own right.
    /// They may serialize as a string: the name of the represented value.
    /// </para>
    /// <para>In this example, an Enum type called Direction is defined:</para>
    ///
    /// <code>
    /// enum Direction {
    ///   NORTH
    ///   EAST
    ///   SOUTH
    ///   WEST
    /// }
    /// </code>
    /// </summary>
    Enum = 8,

    /// <summary>
    /// Scalar types represent primitive leaf values in a GraphQL type system.
    /// GraphQL responses take the form of a hierarchical tree;
    /// the leaves on these trees are GraphQL scalars.
    /// </summary>
    Scalar = 16,

    /// <summary>
    /// Indicates this type is a list. `ofType` is a valid field.
    /// </summary>
    List = 32,

    /// <summary>
    /// Indicates this type is a non-null. `ofType` is a valid field.
    /// </summary>
    NonNull = 64,

    /// <summary>
    /// <para>
    /// A GraphQL schema describes directives which are used to annotate various parts of a
    /// GraphQL document as an indicator that they should be evaluated differently by a
    /// validator, executor, or client tool such as a code generator.
    /// </para>
    /// <para>https://spec.graphql.org/draft/#sec-Type-System.Directives</para>
    /// </summary>
    Directive = 128,
}
