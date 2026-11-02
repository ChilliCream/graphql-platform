namespace HotChocolate.Types.Composite;

/// <summary>
/// <para>
/// The @interfaceObject directive is used within a source schema to declare an object type that
/// acts as a stand-in for an interface defined in another source schema. The stand-in carries the
/// same name as the interface and allows a source schema to contribute fields to that interface
/// without defining any of its implementing types.
/// </para>
/// <para>
/// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--interfaceObject"/>
/// </para>
/// <code>
/// type Media @interfaceObject @key(fields: "id") {
///   id: ID!
///   reviews: [Review!]!
/// }
///
/// directive @interfaceObject on OBJECT
/// </code>
/// </summary>
[DirectiveType(
    DirectiveNames.InterfaceObject.Name,
    DirectiveLocation.Object,
    IsRepeatable = false)]
public sealed class InterfaceObject
{
    private InterfaceObject()
    {
    }

    /// <inheritdoc />
    public override string ToString() => "@interfaceObject";

    /// <summary>
    /// The singleton instance of the <see cref="InterfaceObject"/> directive.
    /// </summary>
    public static InterfaceObject Instance { get; } = new();
}
