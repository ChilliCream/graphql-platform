using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Stitching.Utilities;

public class CopySchemaDefinitionTypeInterceptor : TypeInterceptor
{
    public override void OnAfterCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition)
    {
        if (definition is SchemaTypeDefinition)
        {
            contextData[typeof(RemoteSchemaDefinition).FullName!] =
                completionContext.ContextData[typeof(RemoteSchemaDefinition).FullName!];
        }
    }
}
