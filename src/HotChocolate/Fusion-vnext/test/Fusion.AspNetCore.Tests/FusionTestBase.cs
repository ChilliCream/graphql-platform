using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json;
using HotChocolate.AspNetCore;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Transport.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Xunit.Sdk;

namespace HotChocolate.Fusion;

public abstract class FusionTestBase : IDisposable
{
    private readonly TestServerSession _testServerSession = new();
    private bool _disposed;

    public TestServer CreateSourceSchema(
        string schemaName,
        Action<IRequestExecutorBuilder> configureBuilder,
        Action<IServiceCollection>? configureServices = null,
        Action<IApplicationBuilder>? configureApplication = null,
        Action<HttpClient>? configureHttpClient = null,
        bool isOffline = false,
        bool isTimingOut = false)
    {
        configureApplication ??=
            app =>
            {
                app.UseWebSockets();
                app.UseRouting();
                app.UseEndpoints(endpoint => endpoint.MapGraphQL(schemaName: schemaName));
            };

        return _testServerSession.CreateServer(
            services =>
            {
                services.AddRouting();
                var builder = services.AddGraphQLServer(schemaName);
                configureBuilder(builder);
                configureServices?.Invoke(services);

                services.Configure<SourceSchemaOptions>(opt =>
                {
                    opt.IsOffline = isOffline;
                    opt.IsTimingOut = isTimingOut;
                    opt.ConfigureHttpClient = configureHttpClient;
                });
            },
            configureApplication);
    }

    public async Task<TestServer> CreateCompositeSchemaAsync(
        (string SchemaName, TestServer Server)[] sourceSchemaServers,
        Action<IServiceCollection>? configureServices = null,
        Action<IApplicationBuilder>? configureApplication = null,
        Action<IFusionGatewayBuilder>? configureGatewayBuilder = null,
        [StringSyntax("json")] string? schemaSettings = null)
    {
        var sourceSchemas = new List<SourceSchemaText>();
        var gatewayServices = new ServiceCollection();
        var gatewayBuilder = gatewayServices.AddGraphQLGatewayServer();

        foreach (var (name, server) in sourceSchemaServers)
        {
            var schemaDocument = await server.Services.GetSchemaAsync(name);
            sourceSchemas.Add(new SourceSchemaText(name, schemaDocument.ToString()));

            var sourceSchemaOptions = server.Services.GetRequiredService<IOptions<SourceSchemaOptions>>().Value;
            AddHttpClient(
                gatewayServices,
                name,
                server,
                sourceSchemaOptions);

            if (schemaSettings is null)
            {
                gatewayBuilder.AddHttpClientConfiguration(name, new Uri("http://localhost:5000/graphql"));
            }
        }

        var compositionLog = new CompositionLog();
        var composerOptions = new SchemaComposerOptions { EnableGlobalObjectIdentification = true };
        var composer = new SchemaComposer(sourceSchemas, composerOptions, compositionLog);
        var result = composer.Compose();

        if (!result.IsSuccess)
        {
            var sb = new StringBuilder();
            sb.Append(result.Errors[0].Message);

            foreach (var entry in compositionLog)
            {
                sb.AppendLine();
                sb.Append(entry.Message);
            }

            throw new XunitException(sb.ToString());
        }

        JsonDocumentOwner? settings = null;
        if (schemaSettings is not null)
        {
            var body = JsonDocument.Parse(schemaSettings);
            settings = new JsonDocumentOwner(body, new EmptyMemoryOwner());
        }

        gatewayBuilder.AddInMemoryConfiguration(result.Value.ToSyntaxNode(), settings);
        gatewayBuilder.AddHttpRequestInterceptor<OperationPlanHttpRequestInterceptor>();
        gatewayBuilder.ModifyRequestOptions(o => o.CollectOperationPlanTelemetry = false);
        configureGatewayBuilder?.Invoke(gatewayBuilder);

        configureApplication ??=
            app =>
            {
                app.UseWebSockets();
                app.UseRouting();
                app.UseEndpoints(endpoint => endpoint.MapGraphQL());
            };

        return _testServerSession.CreateServer(
            services =>
            {
                services.AddRouting();

                foreach (var serviceDescriptor in gatewayServices)
                {
                    services.Add(serviceDescriptor);
                }

                configureServices?.Invoke(services);
            },
            configureApplication);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _testServerSession.Dispose();
        }
    }

    private sealed class SourceSchemaOptions
    {
        public bool IsOffline { get; set; }

        public bool IsTimingOut { get; set; }

        public Action<HttpClient>? ConfigureHttpClient { get; set; }
    }

    private sealed class OperationPlanHttpRequestInterceptor : DefaultHttpRequestInterceptor
    {
        public override ValueTask OnCreateAsync(
            HttpContext context,
            IRequestExecutor requestExecutor,
            OperationRequestBuilder requestBuilder,
            CancellationToken cancellationToken)
        {
            requestBuilder.TryAddGlobalState(ExecutionContextData.IncludeOperationPlan, true);
            return base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
        }
    }

     private static IServiceCollection AddHttpClient(
        IServiceCollection services,
        string name,
        TestServer server,
        SourceSchemaOptions options)
    {
        services.TryAddSingleton<IHttpClientFactory, Factory>();
        return services.AddSingleton(new TestServerRegistration(name, server, options));
    }

    private class Factory : IHttpClientFactory
    {
        private readonly Dictionary<string, TestServerRegistration> _registrations;

        public Factory(IEnumerable<TestServerRegistration> registrations)
        {
            _registrations = registrations.ToDictionary(r => r.Name, r => r);
        }

        public HttpClient CreateClient(string name)
        {
            if (_registrations.TryGetValue(name, out var registration))
            {
                HttpClient client;

                if (registration.Options.IsOffline)
                {
                    client = new HttpClient(new ErrorHandler());
                }
                else if (registration.Options.IsTimingOut)
                {
                    client = new HttpClient(new TimeoutHandler());
                }
                else
                {
                    client = registration.Server.CreateClient();
                }

                registration.Options.ConfigureHttpClient?.Invoke(client);

                client.DefaultRequestHeaders.AddGraphQLPreflight();

                return client;
            }

            throw new InvalidOperationException(
                $"No test server registered with the name: {name}");
        }

        private class ErrorHandler : HttpClientHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
                => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }

        private class TimeoutHandler : HttpClientHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);

                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        }
    }

    private record TestServerRegistration(
        string Name,
        TestServer Server,
        SourceSchemaOptions Options);

    private class EmptyMemoryOwner : IMemoryOwner<byte>
    {
        public Memory<byte> Memory => default;

        public void Dispose() { }
    }
}
