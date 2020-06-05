using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Types;

namespace HotChocolate.ApolloFederation
{
    public class ProvidesDirectiveType
        : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name(FederationDirectives.Provides);

            descriptor.Description(FederationResources.ProvidesDirective_Description);

            descriptor.Argument(FederationDirectives.FieldsArgument).Type<NonNullType<FieldSetType>>();

            descriptor.Location(DirectiveLocation.FieldDefinition);
        }
    }

}