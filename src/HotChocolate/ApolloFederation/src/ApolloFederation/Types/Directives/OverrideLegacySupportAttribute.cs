using HotChocolate.Types.Descriptors;

namespace HotChocolate.ApolloFederation.Types;

internal sealed class OverrideLegacySupportAttribute : DirectiveTypeDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IDirectiveTypeDescriptor descriptor,
        Type type)
    {
        // prior to version 2.7 @override only specified "from" parameter
        if (descriptor.GetFederationVersion() < FederationVersion.Federation27)
        {
            var desc = (IDirectiveTypeDescriptor<OverrideDirective>)descriptor;
            desc.BindArgumentsExplicitly();
            desc.Argument(t => t.From);
        }
    }
}
