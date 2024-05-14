using HotChocolate.Types.Descriptors;

namespace HotChocolate.ApolloFederation.Types;

internal sealed class KeyLegacySupportAttribute : DirectiveTypeDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IDirectiveTypeDescriptor descriptor,
        Type type)
    {
        // federation v1 @key only specified "fields" parameter
        if (descriptor.GetFederationVersion() == FederationVersion.Federation10)
        {
            var desc = (IDirectiveTypeDescriptor<KeyDirective>)descriptor;
            desc.BindArgumentsExplicitly();
            desc.Argument(t => t.Fields);
        }
    }
}
