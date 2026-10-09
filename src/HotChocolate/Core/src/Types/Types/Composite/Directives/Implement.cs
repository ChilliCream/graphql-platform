namespace HotChocolate.Types.Composite;

/// <summary>
/// <para>
/// The @implement directive marks a field as an explicit implementation that intentionally replaces
/// a default field implementation contributed to an interface by an @interfaceObject stand-in. It is
/// required on an implementing type's field, or on a more specific interface's own stand-in field,
/// that collides with an applicable default.
/// </para>
/// <para>
/// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--implement"/>
/// </para>
/// <code>
/// type Chair implements Product {
///   id: ID!
///   taxRate: Float! @implement
/// }
///
/// directive @implement on FIELD_DEFINITION
/// </code>
/// </summary>
[DirectiveType(
    DirectiveNames.Implement.Name,
    DirectiveLocation.FieldDefinition,
    IsRepeatable = false)]
public sealed class Implement
{
    private Implement()
    {
    }

    /// <inheritdoc />
    public override string ToString() => "@implement";

    /// <summary>
    /// The singleton instance of the <see cref="Implement"/> directive.
    /// </summary>
    public static Implement Instance { get; } = new();
}
