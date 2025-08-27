using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using HotChocolate.AspNetCore;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
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
        bool isOffline = false)
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

                services.Configure<SourceSchemaOptions>(opt => opt.IsOffline = isOffline);
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

            var subgraphOptions = server.Services.GetRequiredService<IOptions<SourceSchemaOptions>>().Value;
            gatewayServices.AddHttpClient(name, server, subgraphOptions.IsOffline);

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
        gatewayBuilder.ModifyRequestOptions(o =>
        {
            o.AllowOperationPlanRequests = true;
            o.CollectOperationPlanTelemetry = false;
        });
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

    private class EmptyMemoryOwner : IMemoryOwner<byte>
    {
        public Memory<byte> Memory => default;

        public void Dispose() { }
    }
}
