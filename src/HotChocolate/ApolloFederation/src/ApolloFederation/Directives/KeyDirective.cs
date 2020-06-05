using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Types;

namespace HotChocolate.ApolloFederation
{
    public class KeyDirectiveType
        : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name(FederationDirectives.Key);

            descriptor.Description(FederationResources.KeyDirective_Description);

            descriptor.Argument(FederationDirectives.FieldsArgument).Type<NonNullType<FieldSetType>>();

            descriptor.Location(DirectiveLocation.Object | DirectiveLocation.Interface);
        }
    }

}