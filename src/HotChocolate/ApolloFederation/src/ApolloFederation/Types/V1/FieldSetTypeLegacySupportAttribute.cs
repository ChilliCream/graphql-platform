using HotChocolate.Types.Descriptors;

namespace HotChocolate.ApolloFederation.Types;

internal sealed class FieldSetTypeLegacySupportAttribute : ScalarTypeDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IScalarTypeDescriptor descriptor,
        Type type)
    {
        // federation v1 uses a different name for the scalar.
        if (descriptor.GetFederationVersion() == FederationVersion.Federation10)
        {
            descriptor.Extend().Definition.Name = FederationTypeNames.LegacyFieldSetType_Name;
        }
    }
}
