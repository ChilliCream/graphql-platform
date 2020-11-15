using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration
{
    public class SchemaInterceptor : ISchemaInterceptor
    {
        protected SchemaInterceptor()
        {
        }

        public virtual void OnBeforeCreate(
            IDescriptorContext context,
            ISchemaBuilder schemaBuilder)
        {
        }

        public virtual void OnAfterCreate(
            IDescriptorContext context,
            ISchema schema)
        {
        }

        public virtual void OnError(
            IDescriptorContext context,
            Exception exception)
        {
        }
    }
}
