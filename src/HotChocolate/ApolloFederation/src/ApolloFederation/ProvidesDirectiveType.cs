using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Types;

namespace HotChocolate.ApolloFederation
{
    /// <summary>
    /// The @provides directive is used to annotate the expected returned fieldset
    /// from a field on a base type that is guaranteed to be selectable by the gateway.
    ///
    /// <example>
    /// type Review @key(fields: "id") {
    ///   product: Product @provides(fields: "name")
    /// }
    ///
    /// extend type Product @key(fields: "upc") {
    ///   upc: String @external
    ///   name: String @external
    /// }
    /// </example>
    /// </summary>
    public sealed class ProvidesDirectiveType : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor
                .Name(WellKnownTypeNames.Provides)
                .Description(FederationResources.ProvidesDirective_Description)
                .Location(DirectiveLocation.FieldDefinition)
                .FieldsArgument();
        }
    }
}
