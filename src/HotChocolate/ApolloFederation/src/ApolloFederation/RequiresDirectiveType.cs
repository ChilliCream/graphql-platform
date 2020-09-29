using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Types;

namespace HotChocolate.ApolloFederation
{
    /// <summary>
    /// The @requires directive is used to annotate the required input fieldset
    /// from a base type for a resolver. It is used to develop a query plan
    /// where the required fields may not be needed by the client, but the
    /// service may need additional information from other services.
    ///
    /// <example>
    /// # extended from the Users service
    /// extend type User @key(fields: "id") {
    ///   id: ID! @external
    ///   email: String @external
    ///   reviews: [Review] @requires(fields: "email")
    /// }
    /// </example>
    /// </summary>
    public sealed class RequiresDirectiveType
        : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor
                .Name(WellKnownTypeNames.Requires)
                .Description(FederationResources.RequiresDirective_Description)
                .Location(DirectiveLocation.FieldDefinition)
                .FieldsArgument();
        }
    }
}
