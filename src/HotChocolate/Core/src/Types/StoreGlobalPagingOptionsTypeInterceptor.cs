#nullable enable

using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Pagination;

namespace HotChocolate;

internal sealed class StoreGlobalPagingOptionsTypeInterceptor : TypeInterceptor
{
    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration definition)
    {
        if(definition is SchemaTypeDefinition schemaDef)
        {
            var options = completionContext.DescriptorContext.GetPagingOptions(null);
            schemaDef.Features.Set(options);
        }
    }
}
