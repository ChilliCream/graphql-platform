using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration
{
    public interface ISchemaInterceptor
    {
        void OnBeforeCreate(IDescriptorContext context, ISchemaBuilder schemaBuilder);

        void OnAfterCreate(IDescriptorContext context, ISchema schema);

        void OnError(IDescriptorContext context, Exception exception);
    }
}
