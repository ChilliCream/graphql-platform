using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Internal;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Stitching;
using HotChocolate.Stitching.Merge;
using HotChocolate.Stitching.Merge.Rewriters;
using HotChocolate.Stitching.Pipeline;
using HotChocolate.Stitching.Requests;
using HotChocolate.Stitching.Types;
using HotChocolate.Utilities.Introspection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class HotChocolateStitchingRequestExecutorExtensions
    {
        public static IRequestExecutorBuilder PublishSchemaDefinition(
            this IRequestExecutorBuilder builder,
            Action<IPublishSchemaDefinitionDescriptor> configure)
        {
            var descriptor = new PublishSchemaDefinitionDescriptor();
            configure(descriptor);

            builder
                .AddType<SchemaDefinitionType>()
                .TryAddTypeInterceptor<SchemaDefinitionTypeInterceptor>()
                .TryAddSchemaInterceptor(new SchemaDefinitionSchemaInterceptor(descriptor));

            return builder;
        }
    }
}
