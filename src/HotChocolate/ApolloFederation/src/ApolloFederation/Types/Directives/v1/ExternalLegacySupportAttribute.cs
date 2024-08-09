using HotChocolate.Types.Descriptors;

namespace HotChocolate.ApolloFederation.Types;

internal sealed class ExternalLegacySupportAttribute : DirectiveTypeDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IDirectiveTypeDescriptor descriptor,
        Type type)
    {
        // federation v1 only supported @external on fields
        if (descriptor.GetFederationVersion() == FederationVersion.Federation10)
        {
            var desc = (IDirectiveTypeDescriptor<ExternalDirective>)descriptor;
            desc.Extend().Definition.Locations = DirectiveLocation.FieldDefinition;
        }
    }
}
