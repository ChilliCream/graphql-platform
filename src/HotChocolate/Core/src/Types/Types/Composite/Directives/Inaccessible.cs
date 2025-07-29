#nullable enable

namespace HotChocolate.Types.Composite;

/// <summary>
/// <para>
/// The @inaccessible directive is used to prevent specific type system members
/// from being accessible through the client-facing composite schema,
/// even if they are accessible in the underlying source schemas.
/// </para>
/// <para>
/// This directive is useful for restricting access to type system members that
/// are either irrelevant to the client-facing composite schema or sensitive in nature,
/// such as internal identifiers or fields intended only for backend use.
/// </para>
/// <para>
/// directive @inaccessible on FIELD_DEFINITION
///   | OBJECT | INTERFACE | UNION | ARGUMENT_DEFINITION
///   | SCALAR | ENUM | ENUM_VALUE | INPUT_OBJECT | INPUT_FIELD_DEFINITION
/// </para>
/// <para>
/// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--inaccessible"/>
/// </para>
/// </summary>
[DirectiveType(
    DirectiveNames.Inaccessible.Name,
    DirectiveLocation.FieldDefinition
    | DirectiveLocation.Object
    | DirectiveLocation.Interface
    | DirectiveLocation.Union
    | DirectiveLocation.ArgumentDefinition
    | DirectiveLocation.Scalar
    | DirectiveLocation.Enum
    | DirectiveLocation.EnumValue
    | DirectiveLocation.InputObject
    | DirectiveLocation.InputFieldDefinition,
    IsRepeatable = false)]
public sealed class Inaccessible
{
    private Inaccessible()
    {
    }

    /// <inheritdoc />
    public override string ToString() => "@inaccessible";

    /// <summary>
    /// The singleton instance of the <see cref="Internal"/> directive.
    /// </summary>
    public static Inaccessible Instance { get; } = new();
}
