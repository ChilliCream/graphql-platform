namespace HotChocolate.ApolloFederation;

/// <summary>
/// <code>
/// directive @tag(name: String!) repeatable on FIELD_DEFINITION
///  | OBJECT
///  | INTERFACE
///  | UNION
///  | ENUM
///  | ENUM_VALUE
///  | SCALAR
///  | INPUT_OBJECT
///  | INPUT_FIELD_DEFINITION
///  | ARGUMENT_DEFINITION
/// </code>
///
/// The @tag directive allows users to annotate fields and types with additional metadata information.
/// Tagging is commonly used for creating variants of the supergraph using contracts.
///
/// <example>
/// type Foo @tag(name: "internal") {
///   id: ID!
///   name: String
/// }
/// </example>
/// </summary>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Enum |
    AttributeTargets.Field |
    AttributeTargets.Interface |
    AttributeTargets.Method |
    AttributeTargets.Parameter |
    AttributeTargets.Property |
    AttributeTargets.Struct,
    AllowMultiple = true)]
public sealed class ApolloTagAttribute : Attribute
{
    /// <summary>
    /// Initializes new instance of <see cref="ApolloTagAttribute"/>
    /// </summary>
    /// <param name="name">
    /// Tag metadata value
    /// </param>
    public ApolloTagAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Retrieves tag metadata value
    /// </summary>
    public string Name { get; }
}
