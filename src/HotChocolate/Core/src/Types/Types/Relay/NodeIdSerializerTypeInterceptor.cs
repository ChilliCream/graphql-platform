#nullable enable

using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using static HotChocolate.Types.Relay.NodeConstants;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Types.Relay;

internal sealed class NodeIdSerializerTypeInterceptor : TypeInterceptor
{
    public override void OnBeforeCompleteType(ITypeCompletionContext completionContext, TypeSystemConfiguration configuration)
    {
        if (configuration is SchemaTypeConfiguration schemaTypeDef)
        {
            // we ensure that the serializer type map exists.
            if (!completionContext.DescriptorContext.ContextData.TryGetValue(SerializerTypes, out var value))
            {
                value = new Dictionary<string, Type>();
                completionContext.DescriptorContext.ContextData[SerializerTypes] = value;
            }

            // next we make sure that its preserved on the schema for the runtime.
            schemaTypeDef.Features[SerializerTypes] = value;
        }
    }

    internal override void OnAfterCreateSchemaInternal(IDescriptorContext context, Schema schema)
    {
        // now that the schema is created we will look for the node interface and get all implementations and add
        // the serializer types for the node ids also to the map.
        if (schema.Types.TryGetType<InterfaceType>("Node", out var nodeType))
        {
            foreach (var entityType in schema.GetPossibleTypes(nodeType))
            {
                // we tread cautious as final validation of the schema is not yet run.
                if (entityType.Fields.TryGetField(Id, out var idField))
                {
                    RelayIdFieldHelpers.SetSerializerInfos(
                        context,
                        entityType.Name,
                        idField.Member?.GetReturnType() ?? idField.RuntimeType);
                }
            }
        }
    }
}
