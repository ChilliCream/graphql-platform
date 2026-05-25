namespace HotChocolate.Types;

/// <summary>
/// Defines the possible serialization types for GraphQL scalar values.
/// </summary>
[Flags]
public enum ScalarSerializationType
{
    /// <summary>
    /// No serialization type is defined.
    /// </summary>
    Undefined = 0,

    /// <summary>
    /// The scalar serializes to a string value.
    /// </summary>
    String = 1,

    /// <summary>
    /// The scalar serializes to a boolean value.
    /// </summary>
    Boolean = 2,

    /// <summary>
    /// The scalar serializes to an integer value.
    /// </summary>
    Int = 4,

    /// <summary>
    /// The scalar serializes to a floating-point value.
    /// </summary>
    Float = 8,

    /// <summary>
    /// The scalar serializes to an object value.
    /// </summary>
    Object = 16,

    /// <summary>
    /// The scalar serializes to a list value.
    /// </summary>
    List = 32,

    /// <summary>
    /// The scalar can serialize as any possible primitive type.
    /// </summary>
    Any = String | Boolean | Int | Float | Object | List
}
