using System;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Configuration
{
    public class DelegateSchemaInterceptor : SchemaInterceptor
    {
        private readonly OnBeforeSchemaCreate? _onBeforeCreate;
        private readonly OnAfterSchemaCreate? _onAfterCreate;
        private readonly OnSchemaError? _onError;

        public DelegateSchemaInterceptor(
            OnBeforeSchemaCreate? onBeforeCreate = null,
            OnAfterSchemaCreate? onAfterCreate = null,
            OnSchemaError? onError = null)
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
