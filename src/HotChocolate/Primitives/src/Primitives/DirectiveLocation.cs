namespace HotChocolate.Types;

/// <summary>
/// A Directive can be adjacent to many parts of the GraphQL language,
/// a DirectiveLocation describes one such possible adamancies.
/// </summary>
[Flags]
public enum DirectiveLocation
{
    /// <summary>
    /// Location adjacent to a query operation.
    /// </summary>
    Query = 0x1,

    /// <summary>
    /// Location adjacent to a mutation operation.
    /// </summary>
    Mutation = 0x2,

    /// <summary>
    /// Location adjacent to a subscription operation.
    /// </summary>
    Subscription = 0x4,

    /// <summary>
    /// Location adjacent to a field.
    /// </summary>
    Field = 0x8,

    /// <summary>
    /// Location adjacent to a fragment definition.
    /// </summary>
    FragmentDefinition = 0x10,

    /// <summary>
    /// Location adjacent to a fragment spread.
    /// </summary>
    FragmentSpread = 0x20,

    /// <summary>
    /// Location adjacent to an inline fragment.
    /// </summary>
    InlineFragment = 0x40,

    /// <summary>
    /// Location adjacent to a field.
    /// </summary>
    VariableDefinition = 0x40000,

    /// <summary>
    /// Location adjacent to a schema definition.
    /// </summary>
    Schema = 0x80,

    /// <summary>
    /// Location adjacent to a scalar definition.
    /// </summary>
    Scalar = 0x100,

    /// <summary>
    /// Location adjacent to an object type definition.
    /// </summary>
    Object = 0x200,

    /// <summary>
    /// Location adjacent to a field definition.
    /// </summary>
    FieldDefinition = 0x400,

    /// <summary>
    /// Location adjacent to an argument definition
    /// </summary>
    ArgumentDefinition = 0x800,

    /// <summary>
    /// Location adjacent to an interface definition.
    /// </summary>
    Interface = 0x1000,

    /// <summary>
    /// Location adjacent to a union definition.
    /// </summary>
    Union = 0x2000,

    /// <summary>
    /// Location adjacent to an enum definition.
    /// </summary>
    Enum = 0x4000,

    /// <summary>
    /// Location adjacent to an enum value definition.
    /// </summary>
    EnumValue = 0x8000,

    /// <summary>
    /// Location adjacent to an input object type definition.
    /// </summary>
    InputObject = 0x10000,

    /// <summary>
    /// Location adjacent to an input object field definition.
    /// </summary>
    InputFieldDefinition = 0x20000,

    // see: https://spec.graphql.org/draft/#ExecutableDirectiveLocation
    Executable =
        Query |
        Mutation |
        Subscription |
        Field |
        FragmentDefinition |
        FragmentSpread |
        InlineFragment |
        VariableDefinition,

    // see: https://spec.graphql.org/draft/#TypeSystemDirectiveLocation
    TypeSystem =
        Schema |
        Scalar |
        Object |
        FieldDefinition |
        ArgumentDefinition |
        Interface |
        Union |
        Enum |
        EnumValue |
        InputObject |
        InputFieldDefinition,

    Operation =
        Query |
        Mutation |
        Subscription,

    Fragment =
        InlineFragment |
        FragmentSpread |
        FragmentDefinition,
}
