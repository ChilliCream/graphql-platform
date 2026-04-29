using HotChocolate.Configuration;
using HotChocolate.Serialization;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate;

internal sealed class StoreGlobalSchemaOptionsTypeInterceptor : TypeInterceptor
{
    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration configuration)
    {
        if (configuration is SchemaTypeConfiguration schemaDef)
        {
            var options = completionContext.DescriptorContext.Options;
            schemaDef.Features.Set(options);
            schemaDef.Features.Set(
                new SchemaFormatterOptions
                {
                    OrderFieldsByName = options.SortFieldsByName
                });
        }
    }
}
