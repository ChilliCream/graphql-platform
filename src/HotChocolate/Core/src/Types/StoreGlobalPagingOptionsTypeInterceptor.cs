#nullable enable

using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Pagination;

namespace HotChocolate;

internal sealed class StoreGlobalPagingOptionsTypeInterceptor : TypeInterceptor
{
    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration configuration)
    {
        if (configuration is SchemaTypeConfiguration schemaDef)
        {
            var options = completionContext.DescriptorContext.GetPagingOptions(null);
            schemaDef.Features.Set(options);
        }
    }
}
