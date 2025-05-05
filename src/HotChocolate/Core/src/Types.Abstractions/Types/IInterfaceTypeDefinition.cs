namespace HotChocolate.Types;

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
public interface IInterfaceTypeDefinition : IComplexTypeDefinition;
