using HotChocolate.Types;

namespace HotChocolate.ApolloFederation
{
    public sealed class ExternalDirectiveType
        : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name(FederationDirectives.External);

            descriptor.Description(
                "Directive to indicate that a field is owned " + 
                "by another service, for example via Apollo federation.");

            descriptor
                .Location(DirectiveLocation.FieldDefinition);
        }
    }
}
