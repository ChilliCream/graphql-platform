using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Types;

namespace HotChocolate.ApolloFederation
{
    public class RequiresDirectiveType
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
