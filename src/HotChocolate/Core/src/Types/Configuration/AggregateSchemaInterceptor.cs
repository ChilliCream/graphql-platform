using System;
using System.Collections.Generic;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration
{
    internal sealed class AggregateSchemaInterceptor : SchemaInterceptor
    {
        private readonly IReadOnlyList<ISchemaInterceptor> _interceptors;

        public AggregateSchemaInterceptor()
        {
            _interceptors = Array.Empty<ISchemaInterceptor>();
        }

        public AggregateSchemaInterceptor(IReadOnlyList<ISchemaInterceptor> interceptors)
        {
            _interceptors = interceptors;
        }

        public override void OnBeforeCreate(
            IDescriptorContext context,
            ISchemaBuilder schemaBuilder)
        {
            if (_interceptors.Count == 0)
            {
                return;
            }

            foreach (ISchemaInterceptor interceptor in _interceptors)
            {
                interceptor.OnBeforeCreate(context, schemaBuilder);
            }
        }

        public override void OnAfterCreate(IDescriptorContext context, ISchema schema)
        {
            if (_interceptors.Count == 0)
            {
                return;
            }

            foreach (ISchemaInterceptor interceptor in _interceptors)
            {
                interceptor.OnAfterCreate(context, schema);
            }
        }

        public override void OnError(IDescriptorContext context, Exception exception)
        {
            if (_interceptors.Count == 0)
            {
                return;
            }

            foreach (ISchemaInterceptor interceptor in _interceptors)
            {
                interceptor.OnError(context, exception);
            }
        }
    }
}
