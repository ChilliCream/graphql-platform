using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Stitching.Utilities;

internal sealed class CopySchemaDefinitionTypeInterceptor : TypeInterceptor
{
    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        if (definition is SchemaTypeDefinition schemaTypeDef)
        {
            schemaTypeDef.TouchContextData();
        }
    }

    public override void OnAfterCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        if (definition is SchemaTypeDefinition schemaTypeDef)
        {
            schemaTypeDef.ContextData[typeof(RemoteSchemaDefinition).FullName!] =
                completionContext.ContextData[typeof(RemoteSchemaDefinition).FullName!];
        }
    }
}
