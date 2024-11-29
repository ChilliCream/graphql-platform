namespace HotChocolate.Types;

/// <summary>
/// GraphQL Type System Member Kind
/// </summary>
public enum MemberKind
{
    /// <summary>
    /// GraphQL Interface Field.
    /// </summary>
    InterfaceField,

    /// <summary>
    /// GraphQL Object Field.
    /// </summary>
    ObjectField,

    /// <summary>
    /// GraphQL Input Object Field
    /// </summary>
    InputObjectField,

    /// <summary>
    /// GraphQL Output Field Argument
    /// </summary>
    Argument,

    /// <summary>
    /// GraphQL Directive Argument.
    /// </summary>
    DirectiveArgument,
}
