using HotChocolate.Types.Descriptors;

namespace HotChocolate
{
    public interface ISchemaInterceptor
    {
        void OnBeforeCreate(IDescriptorContext context, ISchemaBuilder schemaBuilder);

        void OnAfterCreate(IDescriptorContext context, ISchema schema);
    }
}
