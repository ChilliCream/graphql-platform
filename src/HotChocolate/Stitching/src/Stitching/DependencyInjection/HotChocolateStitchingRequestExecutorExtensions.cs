using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Stitching;
using HotChocolate.Stitching.Pipeline;
using HotChocolate.Stitching.Requests;
using HotChocolate.Utilities.Introspection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WellKnownContextData = HotChocolate.Stitching.WellKnownContextData;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HotChocolateStitchingRequestExecutorExtensions
    {
        /// <summary>
        /// This middleware delegates GraphQL requests to a different GraphQL server using
        /// GraphQL HTTP requests.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <returns>
        /// Returns the <see cref="IRequestExecutorBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="builder"/> is <c>null</c>.
        /// </exception>
        public static IRequestExecutorBuilder UseHttpRequests(
            this IRequestExecutorBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.UseRequest<HttpRequestMiddleware>();
        }

        public static IRequestExecutorBuilder UseHttpRequestPipeline(
            this IRequestExecutorBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder
                .UseInstrumentations()
                .UseExceptions()
                .UseDocumentCache()
                .UseReadPersistedQuery()
                .UseWritePersistedQuery()
                .UseDocumentParser()
                .UseDocumentValidation()
                .UseOperationCache()
                .UseOperationResolver()
                .UseOperationVariableCoercion()
                .UseHttpRequests();
        }

        public static IRequestExecutorBuilder AddHttpRemoteSchema(
            IRequestExecutorBuilder builder,
            NameString schemaName)
        {
            // first we add a full GraphQL schema and executor that represents the remote schema.
            // This remote schema will be used by the stitching engine to execute queries against
            // this schema and also to lookup types in order correctly convert between scalars.
            builder
                .AddGraphQL(schemaName)
                .ConfigureSchemaAsync(
                    async (services, schemaBuilder, cancellationToken) =>
                    {
                        // The schema will be fetched via HTTP from the downstream service.
                        // We will use the schema name to get a the HttpClient, which
                        // we expect is correctly configured.
                        HttpClient httpClient = services
                            .GetRequiredService<IHttpClientFactory>()
                            .CreateClient(schemaName);

                        // The introspection client will do all the hard work to negotiate
                        // the features this schema supports and convert the introspection
                        // result into a parsed GraphQL SDL document.
                        DocumentNode document = await new IntrospectionClient()
                            .DownloadSchemaAsync(httpClient, cancellationToken)
                            .ConfigureAwait(false);

                        // The document is used to create a SDL-first schema ...
                        schemaBuilder.AddDocument(document);

                        // ... which will fail if any resolver is actually used ...
                        // todo : how bind resolvers
                        schemaBuilder.Use(next => context => throw new NotSupportedException());
                    })
                // ... instead we are using a special request pipeline that does everything like
                // the standard pipeline except the last middleware will not start the execution
                // algorithms but delegate the request via HTTP to the downstream schema.
                .UseHttpRequestPipeline();

            // Next, we will register a request executor proxy with the stitched schema,
            // that the stitching runtime will use to send requests to the schema representing
            // the downstream service.
            builder
                .ConfigureSchemaAsync(async (services, schemaBuilder, cancellationToken) =>
                {
                    var autoProxy = await AutoUpdateRequestExecutorProxy.CreateAsync(
                        new RequestExecutorProxy(
                            services.GetRequiredService<IRequestExecutorResolver>(),
                            schemaName),
                        cancellationToken);

                    schemaBuilder
                        .AddRemoteExecutor(schemaName, autoProxy)
                        .TryAddSchemaInterceptor(typeof(StitchingSchemaInterceptor));

                });

            // Last but not least, we will setup the stitching context which will
            // provide access to the remote executors which in turn use the just configured
            // request executor proxies to send requests to the downstream services.
            builder.Services.TryAddScoped<IStitchingContext, StitchingContext>();

            return builder;
        }
    }
}
