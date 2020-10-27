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

            builder
                .AddType<SchemaDefinitionType>()
                .TryAddTypeInterceptor<SchemaDefinitionTypeInterceptor>()
                .TryAddSchemaInterceptor(new SchemaDefinitionSchemaInterceptor(descriptor));

            return builder;
        }
    }
}
