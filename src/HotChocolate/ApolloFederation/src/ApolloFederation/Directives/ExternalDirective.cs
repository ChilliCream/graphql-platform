using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Types;

namespace HotChocolate.ApolloFederation
{
    public sealed class ExternalDirectiveType
        : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name(FederationDirectives.External);

            descriptor.Description(FederationResources.ExternalDirective_Description);

            descriptor
                .Location(DirectiveLocation.FieldDefinition);
        }
    }
}
