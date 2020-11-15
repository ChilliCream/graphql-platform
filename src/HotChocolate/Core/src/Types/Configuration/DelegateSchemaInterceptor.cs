using System;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Configuration
{
    public class DelegateSchemaInterceptor : SchemaInterceptor
    {
        private readonly Action<IDescriptorContext, ISchemaBuilder>? _onBeforeCreate;
        private readonly Action<IDescriptorContext, ISchema>? _onAfterCreate;
        private readonly Action<IDescriptorContext, Exception>? _onError;

        public DelegateSchemaInterceptor(
            Action<IDescriptorContext, ISchemaBuilder>? onBeforeCreate = null,
            Action<IDescriptorContext, ISchema>? onAfterCreate = null,
            Action<IDescriptorContext, Exception>? onError = null)
        {
            _onAfterCreate = onAfterCreate;
            _onBeforeCreate = onBeforeCreate;
            _onError = onError;
        }

        public override void OnBeforeCreate(
            IDescriptorContext context,
            ISchemaBuilder schemaBuilder) =>
            _onBeforeCreate?.Invoke(context, schemaBuilder);

        public override void OnAfterCreate(
            IDescriptorContext context,
            ISchema schema) =>
            _onAfterCreate?.Invoke(context, schema);

        public override void OnError(
            IDescriptorContext context,
            Exception exception) =>
            _onError?.Invoke(context, exception);
    }
}
