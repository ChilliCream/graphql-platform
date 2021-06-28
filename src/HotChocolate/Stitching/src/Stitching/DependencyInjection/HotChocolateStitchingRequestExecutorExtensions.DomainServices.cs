using System;
using HotChocolate.Execution.Configuration;
using HotChocolate.Stitching.SchemaDefinitions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class HotChocolateStitchingRequestExecutorExtensions
    {
        public static IRequestExecutorBuilder PublishSchemaDefinition(
            this IRequestExecutorBuilder builder,
            Action<IPublishSchemaDefinitionDescriptor> configure)
        {
            var descriptor = new PublishSchemaDefinitionDescriptor(builder);
            configure(descriptor);

            var typeInterceptor = new SchemaDefinitionTypeInterceptor(!descriptor.HasPublisher);
            var schemaInterceptor = new SchemaDefinitionSchemaInterceptor(descriptor);

            builder
                .AddType<SchemaDefinitionType>()
                .TryAddTypeInterceptor(typeInterceptor)
                .TryAddSchemaInterceptor(schemaInterceptor)
                .ConfigureOnRequestExecutorCreatedAsync(
                    async (sp, executor, ct) => await descriptor
                        .PublishAsync(sp, ct)
                        .ConfigureAwait(false));

            return builder;
        }
    }
}
