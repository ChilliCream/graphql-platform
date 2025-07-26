using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public abstract class FusionTestBase : IDisposable
{
    internal readonly TestServerSession _testServerSession = new();
    private bool _disposed;

    public TestServer CreateSourceSchema(
        string schemaName,
        Action<IRequestExecutorBuilder> configureBuilder,
        Action<IServiceCollection>? configureServices = null,
        Action<IApplicationBuilder>? configureApplication = null)
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
            },
            configureApplication);
    }

    public async Task<TestServer> CreateCompositeSchemaAsync(
        (string SchemaName, TestServer Server)[] sourceSchemaServers,
        Action<IServiceCollection>? configureServices = null,
        Action<IApplicationBuilder>? configureApplication = null)
    {
        var sourceSchemas = new List<string>();
        var gatewayServices = new ServiceCollection();
        var gatewayBuilder = gatewayServices.AddGraphQLGatewayServer();

        foreach (var (name, server) in sourceSchemaServers)
        {
            var schemaDocument = await server.Services.GetSchemaAsync(name);
            sourceSchemas.Add(schemaDocument.ToString());
            gatewayServices.AddHttpClient(name, server);
            gatewayBuilder.AddHttpClientConfiguration(name, new Uri("http://localhost:5000/graphql"));
        }

        var compositionLog = new CompositionLog();
        var composer = new SchemaComposer(sourceSchemas, new SchemaComposerOptions(), compositionLog);
        var result = composer.Compose();

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Errors[0].Message);
        }

        gatewayBuilder.AddInMemoryConfiguration(result.Value.ToSyntaxNode());

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
}
