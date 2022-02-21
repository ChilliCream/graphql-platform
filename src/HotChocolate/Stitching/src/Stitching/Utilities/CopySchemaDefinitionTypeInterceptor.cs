using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Stitching.Utilities;

internal sealed class CopySchemaDefinitionTypeInterceptor : TypeInterceptor
{
    public override void OnAfterCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData)
    {
        if (definition is SchemaTypeDefinition)
        {
            contextData[typeof(RemoteSchemaDefinition).FullName!] =
                completionContext.ContextData[typeof(RemoteSchemaDefinition).FullName!];
        }
    }
}
