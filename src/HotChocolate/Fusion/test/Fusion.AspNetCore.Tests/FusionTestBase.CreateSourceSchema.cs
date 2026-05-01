using System.Collections.Immutable;
using System.Net.Http.Headers;
using HotChocolate.AspNetCore;
using HotChocolate.Configuration;
using HotChocolate.Execution.Configuration;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Language;
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
        bool isOffline = false,
        bool isTimingOut = false,
        SourceSchemaClientCapabilities capabilities = SourceSchemaClientCapabilities.All,
        ErrorHandlingMode? onError = null,
        ImmutableArray<MediaTypeWithQualityHeaderValue>? defaultAcceptHeaderValues = null,
        ImmutableArray<MediaTypeWithQualityHeaderValue>? batchingAcceptHeaderValues = null,
        ImmutableArray<MediaTypeWithQualityHeaderValue>? subscriptionAcceptHeaderValues = null,
        Func<HttpRequestMessage, Task<HttpResponseMessage>>? mockHttpResponse = null)
    {
        configureApplication ??=
            app =>
            {
                app.UseWebSockets();
                app.UseRouting();
                app.UseEndpoints(endpoint =>
                    endpoint.MapGraphQL(schemaName: schemaName));
            };

        return _testServerSession.CreateServer(
            services =>
            {
                services.AddRouting();
                var builder = services.AddGraphQLServer(schemaName, disableDefaultSecurity: true);
                builder.AddSourceSchemaDefaults();
                builder.ModifyServerOptions(o => o.Batching = AllowedBatching.All);
                configureBuilder(builder);
                configureServices?.Invoke(services);

                services.Configure<SourceSchemaOptions>(opt =>
                {
                    opt.IsOffline = isOffline;
                    opt.IsTimingOut = isTimingOut;
                    opt.ConfigureHttpClient = configureHttpClient;
                    opt.MockHttpResponse = mockHttpResponse;
                    opt.Capabilities = capabilities;
                    opt.OnError = onError;
                    opt.DefaultAcceptHeaderValues = defaultAcceptHeaderValues;
                    opt.BatchingAcceptHeaderValues = batchingAcceptHeaderValues;
                    opt.SubscriptionAcceptHeaderValues = subscriptionAcceptHeaderValues;
                });
            },
            configureApplication);
    }

    protected TestServer CreateSourceSchema(
        string schemaName,
        string schemaText,
        bool isOffline = false,
        bool isTimingOut = false,
        Action<HttpClient>? configureHttpClient = null,
        HttpClient? httpClient = null,
        SourceSchemaClientCapabilities capabilities = SourceSchemaClientCapabilities.All,
        ErrorHandlingMode? onError = null,
        ImmutableArray<MediaTypeWithQualityHeaderValue>? defaultAcceptHeaderValues = null,
        ImmutableArray<MediaTypeWithQualityHeaderValue>? batchingAcceptHeaderValues = null,
        ImmutableArray<MediaTypeWithQualityHeaderValue>? subscriptionAcceptHeaderValues = null,
        Func<HttpRequestMessage, Task<HttpResponseMessage>>? mockHttpResponse = null)
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
                    .AddTestDirectives()
                    .ModifyServerOptions(o => o.Batching = AllowedBatching.All);

                services.Configure<SourceSchemaOptions>(opt =>
                {
                    opt.IsOffline = isOffline;
                    opt.IsTimingOut = isTimingOut;
                    opt.ConfigureHttpClient = configureHttpClient;
                    opt.HttpClient = httpClient;
                    opt.MockHttpResponse = mockHttpResponse;
                    opt.Capabilities = capabilities;
                    opt.OnError = onError;
                    opt.DefaultAcceptHeaderValues = defaultAcceptHeaderValues;
                    opt.BatchingAcceptHeaderValues = batchingAcceptHeaderValues;
                    opt.SubscriptionAcceptHeaderValues = subscriptionAcceptHeaderValues;
                });
            },
            app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoint =>
                    endpoint.MapGraphQL(schemaName: schemaName));
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
