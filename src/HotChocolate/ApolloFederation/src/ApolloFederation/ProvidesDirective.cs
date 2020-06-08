using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Types;

namespace HotChocolate.ApolloFederation
{
    /// <summary>
    /// The @provides directive is used to annotate the expected returned fieldset 
    /// from a field on a base type that is guaranteed to be selectable by the gateway.
    /// </summary>
    public class ProvidesDirectiveType
        : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor
                .Name(TypeNames.Provides)
                .Description(FederationResources.ProvidesDirective_Description)
                .Location(DirectiveLocation.FieldDefinition)
                .FieldsArgument();
        }
    }
}
