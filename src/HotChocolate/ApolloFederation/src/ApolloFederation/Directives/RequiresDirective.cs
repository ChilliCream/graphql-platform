using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Types;

namespace HotChocolate.ApolloFederation
{
    public class RequiresDirectiveType
        : DirectiveType
    {
      protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name(FederationDirectives.Requires);

            descriptor.Description(FederationResources.RequiresDirective_Description);

            descriptor.Argument(FederationDirectives.FieldsArgument).Type<NonNullType<FieldSetType>>();

            descriptor
                .Location(DirectiveLocation.FieldDefinition);
        }
    }
}