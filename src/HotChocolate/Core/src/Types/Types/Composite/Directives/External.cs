namespace HotChocolate.Types.Composite;

/// <summary>
/// <para>
/// The @external directive indicates that a field is recognized by the current source schema but is
/// not directly contributed (resolved) by it. Instead, the source schema references the field for
/// specific composition purposes, for example as part of a @requires selection.
/// </para>
/// <para>
/// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--external"/>
/// </para>
/// <code>
/// type User @key(fields: "id") {
///   id: ID!
///   name: String! @external
///   username: String! @requires(fields: "name")
/// }
///
/// directive @external on FIELD_DEFINITION
/// </code>
/// </summary>
[DirectiveType(
    DirectiveNames.External.Name,
    DirectiveLocation.FieldDefinition,
    IsRepeatable = false)]
public sealed class External
{
    private External()
    {
    }

    /// <inheritdoc />
    public override string ToString() => "@external";

    /// <summary>
    /// The singleton instance of the <see cref="External"/> directive.
    /// </summary>
    public static External Instance { get; } = new();
}
