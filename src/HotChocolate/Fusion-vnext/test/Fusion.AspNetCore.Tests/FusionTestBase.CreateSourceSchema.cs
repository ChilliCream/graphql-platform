using System.Collections.Immutable;
using System.Net.Http.Headers;
using HotChocolate.AspNetCore;
using HotChocolate.Configuration;
using HotChocolate.Execution.Configuration;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Types.Descriptors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Composite = HotChocolate.Types.Composite;

namespace HotChocolate.Fusion;

public abstract partial class FusionTestBase
{
    protected TestServer CreateSourceSchema(
        string schemaName,
        Action<IRequestExecutorBuilder> configureBuilder,
        Action<IServiceCollection>? configureServices = null,
        Action<IApplicationBuilder>? configureApplication = null,
        Action<HttpClient>? configureHttpClient = null,
        SourceSchemaHttpClientBatchingMode batchingMode = SourceSchemaHttpClientBatchingMode.VariableBatching,
        ImmutableArray<MediaTypeWithQualityHeaderValue>? batchingAcceptHeaderValues = null,
        bool isOffline = false,
        bool isTimingOut = false)
    {
        configureApplication ??=
            app =>
            {
                app.UseWebSockets();
                app.UseRouting();
                app.UseEndpoints(endpoint =>
                    endpoint.MapGraphQL(schemaName: schemaName)
                        .WithOptions(new GraphQLServerOptions { EnableBatching = true }));
            };

        return _testServerSession.CreateServer(
            services =>
            {
                services.AddRouting();
                var builder = services.AddGraphQLServer(schemaName, disableDefaultSecurity: true);
                builder.AddSourceSchemaDefaults();
                configureBuilder(builder);
                configureServices?.Invoke(services);

                services.Configure<SourceSchemaOptions>(opt =>
                {
                    opt.IsOffline = isOffline;
                    opt.IsTimingOut = isTimingOut;
                    opt.ConfigureHttpClient = configureHttpClient;
                    opt.BatchingMode = batchingMode;
                    opt.BatchingAcceptHeaderValues = batchingAcceptHeaderValues;
                });
            },
            configureApplication);
    }

    protected TestServer CreateSourceSchema(
        string schemaName,
        string schemaText,
        bool isOffline = false,
        bool isTimingOut = false,
        SourceSchemaHttpClientBatchingMode batchingMode = SourceSchemaHttpClientBatchingMode.VariableBatching,
        ImmutableArray<MediaTypeWithQualityHeaderValue>? batchingAcceptHeaderValues = null,
        Action<HttpClient>? configureHttpClient = null,
        HttpClient? httpClient = null)
    {
        return _testServerSession.CreateServer(services =>
            {
                services.AddRouting();

                services.AddGraphQLServer(schemaName, disableDefaultSecurity: true)
                    .AddType<Composite.FieldSelectionSetType>()
                    .AddType<Composite.FieldSelectionMapType>()
                    .TryAddTypeInterceptor<RegisterFusionDirectivesTypeInterceptor>()
                    .AddDocumentFromString(schemaText)
                    .AddResolverMocking()
                    .AddTestDirectives();

                services.Configure<SourceSchemaOptions>(opt =>
                {
                    opt.IsOffline = isOffline;
                    opt.IsTimingOut = isTimingOut;
                    opt.ConfigureHttpClient = configureHttpClient;
                    opt.HttpClient = httpClient;
                    opt.BatchingMode = batchingMode;
                    opt.BatchingAcceptHeaderValues = batchingAcceptHeaderValues;
                });
            },
            app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoint =>
                {
                    endpoint.MapGraphQL(schemaName: schemaName)
                        .WithOptions(new GraphQLServerOptions { EnableBatching = true });
                });
            });
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class RegisterFusionDirectivesTypeInterceptor : TypeInterceptor
    {
        private bool _registeredTypes;

        public override IEnumerable<TypeReference> RegisterMoreTypes(
            IReadOnlyCollection<ITypeDiscoveryContext> discoveryContexts)
        {
            if (!_registeredTypes)
            {
                var typeInspector = discoveryContexts.First().DescriptorContext.TypeInspector;

                yield return typeInspector.GetTypeRef(typeof(Composite.Lookup));
                yield return typeInspector.GetTypeRef(typeof(Composite.Internal));
                yield return typeInspector.GetTypeRef(typeof(Composite.Inaccessible));
                yield return typeInspector.GetTypeRef(typeof(Composite.EntityKey));
                yield return typeInspector.GetTypeRef(typeof(Composite.Require));
                yield return typeInspector.GetTypeRef(typeof(Composite.Is));
                yield return typeInspector.GetTypeRef(typeof(Composite.Provides));
                yield return typeInspector.GetTypeRef(typeof(Composite.Shareable));

                _registeredTypes = true;
            }
        }
    }
}
