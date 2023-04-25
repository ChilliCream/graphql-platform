using HotChocolate.ApolloFederation.Constants;
using HotChocolate.ApolloFederation.Properties;

namespace HotChocolate.ApolloFederation;

/// <summary>
/// The @tag directive is used to applies arbitrary string
/// metadata to a schema location. Custom tooling can use
/// this metadata during any step of the schema delivery flow,
/// including composition, static analysis, and documentation
///
/// <example>
/// # extended from the Users service
/// extend type User @key(fields: "id") {
///   id: ID! @external
///   email: String @tag(name: "public")
///   customerNotes: String @tag(name: "internal")
/// }
/// </example>
/// </summary>
public sealed class TagDirective
{
    public TagDirective(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public string Name { get; }
}

/// <summary>
/// The @tag directive is used to applies arbitrary string
/// metadata to a schema location. Custom tooling can use
/// this metadata during any step of the schema delivery flow,
/// including composition, static analysis, and documentation
///
/// <example>
/// # extended from the Users service
/// extend type User @key(fields: "id") {
///   id: ID! @external
///   email: String @tag(name: "public")
///   customerNotes: String @tag(name: "internal")
/// }
/// </example>
/// </summary>
public sealed class TagDirectiveType : DirectiveType<TagDirective>
{
    protected override void Configure(IDirectiveTypeDescriptor<TagDirective> descriptor)
        => descriptor
            .Name(WellKnownTypeNames.Tag)
            .Description(FederationResources.TagDirective_Description)
            .Location(
                DirectiveLocation.FieldDefinition |
                DirectiveLocation.Interface |
                DirectiveLocation.Object |
                DirectiveLocation.Union |
                DirectiveLocation.ArgumentDefinition |
                DirectiveLocation.Scalar |
                DirectiveLocation.Enum |
                DirectiveLocation.EnumValue |
                DirectiveLocation.InputObject |
                DirectiveLocation.InputFieldDefinition)
            .Repeatable();
}
