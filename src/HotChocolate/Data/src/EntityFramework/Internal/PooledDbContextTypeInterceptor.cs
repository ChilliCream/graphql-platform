using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Data.Internal.EntityFrameworkContextData;
using static HotChocolate.Types.EntityFrameworkObjectFieldDescriptorExtensions;

namespace HotChocolate.Data.Internal;

internal sealed class PooledDbContextTypeInterceptor : TypeInterceptor
{
    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData)
    {
        if (definition is ObjectTypeDefinition typeDef)
        {
            foreach (ObjectFieldDefinition field in typeDef.Fields)
            {
                if (field.ContextData.TryGetValue(DbContextType, out var value) &&
                    value is Type dbContextType)
                {
                    UseDbContext(field, dbContextType);
                }
            }
        }
    }
}
