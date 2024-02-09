#nullable enable
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Interceptors;

internal sealed class DisableNullBubblingTypeInterceptor : TypeInterceptor
{
    internal override bool IsEnabled(IDescriptorContext context)
        => context.Options.DisableNullBubbling;

    public override void OnAfterInitialize(ITypeDiscoveryContext discoveryContext, DefinitionBase definition)
    {
        if (definition is SchemaTypeDefinition schemaDef)
        {
            schemaDef.ContextData[WellKnownContextData.DisableNullBubbling] = true;
        }
    }
}
