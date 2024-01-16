using HotChocolate.ApolloFederation.Constants;
using HotChocolate.ApolloFederation.Properties;

namespace HotChocolate.ApolloFederation;

/// <summary>
/// <code>
/// directive @extends on OBJECT | INTERFACE
/// </code>
///
/// The @extends directive is used to represent type extensions in the schema. Federated extended types should have
/// corresponding @key directive defined that specifies primary key required to fetch the underlying object.
/// <example>
/// # extended from the Users service
/// type Foo @extends @key(fields: "id") {
///   id: ID
///   description: String
/// }
/// </example>
/// </summary>
public sealed class ExtendServiceTypeDirectiveType : DirectiveType
{
    protected override void Configure(IDirectiveTypeDescriptor descriptor)
    {
        descriptor
            .Name(WellKnownTypeNames.Extends)
            .Description(FederationResources.ExtendsDirective_Description)
            .Location(DirectiveLocation.Object | DirectiveLocation.Interface);
    }
}
