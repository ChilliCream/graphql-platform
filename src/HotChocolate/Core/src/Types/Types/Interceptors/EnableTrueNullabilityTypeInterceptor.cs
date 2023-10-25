#nullable enable
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Interceptors;

internal sealed class EnableTrueNullabilityTypeInterceptor : TypeInterceptor
{
    internal override bool IsEnabled(IDescriptorContext context)
        => context.Options.EnableTrueNullability;

    public override void OnAfterInitialize(ITypeDiscoveryContext discoveryContext, DefinitionBase definition)
    {
        if (definition is SchemaTypeDefinition schemaDef)
        {
            schemaDef.ContextData[WellKnownContextData.EnableTrueNullability] = true;
        }
    }
}