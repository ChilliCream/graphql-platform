using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json;
using HotChocolate.AspNetCore;
using HotChocolate.Buffers;
using HotChocolate.Configuration;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Language;
using HotChocolate.Transport.Http;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Descriptors;
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
                var builder = services.AddGraphQLServer(schemaName, disableDefaultSecurity: true);
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

    public TestServer CreateSourceSchema(
        string schemaName,
        string schemaText,
        bool isOffline = false,
        bool isTimingOut = false)
    {
        return _testServerSession.CreateServer(services =>
        {
            services.AddRouting();

            services.AddGraphQLServer(schemaName, disableDefaultSecurity: true)
                .AddType<FieldSelectionSetType>()
                .AddType<FieldSelectionMapType>()
                .TryAddTypeInterceptor<RegisterFusionDirectivesTypeInterceptor>()
                .AddDocumentFromString(schemaText)
                .AddResolverMocking()
                .AddTestDirectives();

            services.Configure<SourceSchemaOptions>(opt =>
            {
                opt.IsOffline = isOffline;
                opt.IsTimingOut = isTimingOut;
            });
        },
        app =>
        {
            app.UseRouting();
            app.UseEndpoints(endpoint => endpoint.MapGraphQL(schemaName: schemaName));
        });
    }

    public async Task<Gateway> CreateCompositeSchemaAsync(
        (string SchemaName, TestServer Server)[] sourceSchemaServers,
        Action<IServiceCollection>? configureServices = null,
        Action<IApplicationBuilder>? configureApplication = null,
        Action<IFusionGatewayBuilder>? configureGatewayBuilder = null,
        [StringSyntax("json")] string? schemaSettings = null)
    {
        var sourceSchemas = new List<SourceSchemaText>();
        var gatewayServices = new ServiceCollection();
        var gatewayBuilder = gatewayServices.AddGraphQLGatewayServer();
        var interactions = new ConcurrentDictionary<string, ConcurrentDictionary<int, RequestResponsePair>>();

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
                gatewayBuilder.AddHttpClientConfiguration(
                    name,
                    new Uri("http://localhost:5000/graphql"),
                    onBeforeSend: (context, node, request) =>
                    {
                        if (request.Content == null)
                        {
                            return;
                        }

                        var originalStream = request.Content.ReadAsStream();

                        using var memoryStream = new MemoryStream();
                        originalStream.CopyTo(memoryStream);

                        if (originalStream.CanSeek)
                        {
                            originalStream.Position = 0;
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }

                        memoryStream.Position = 0;
                        using var reader = new StreamReader(memoryStream, leaveOpen: true);
                        var content = reader.ReadToEnd();

                        var schemaName = node is OperationExecutionNode { SchemaName: { } staticSchemaName }
                            ? staticSchemaName
                            : context.GetDynamicSchemaName(node);

                        var schemaInteractions = interactions.GetOrAdd(schemaName, _ => []);
                        var pair = schemaInteractions.GetOrAdd(node.Id, _ => new RequestResponsePair());

                        pair.Request = content;
                    }
                    // onAfterReceive: (context, node, response) =>
                    // {
                    //     var originalStream =  response.Content.ReadAsStream();
                    //     using var reader = new StreamReader(originalStream, leaveOpen: true);
                    //     var content = reader.ReadToEnd();
                    //
                    //     // TODO: Make this properly work
                    //
                    //     // response.Content = new StringContent(content);
                    //
                    //     var schemaName = node is OperationExecutionNode { SchemaName: { } staticSchemaName }
                    //         ? staticSchemaName
                    //         : context.GetDynamicSchemaName(node);
                    //
                    //     var schemaInteractions = interactions.GetOrAdd(schemaName, _ => []);
                    //     var pair = schemaInteractions.GetOrAdd(node.Id, _ => new RequestResponsePair());
                    //
                    //     pair.Response = content;
                    // }
                    );
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

        var gatewayTestServer = _testServerSession.CreateServer(
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

        return new Gateway(gatewayTestServer, sourceSchemas, interactions);
    }

    // TODO: We should strip fusion directive definitions from source schema text before printing
    // TODO: Properly print interactions and offline, etc status for source schema
    protected void MatchSnapshot(
        Gateway gateway,
        HotChocolate.Transport.OperationRequest request,
        HotChocolate.Transport.OperationResult response)
    {
        var snapshot = new Snapshot(extension: ".yaml");

        var sb = new StringBuilder();
        var writer = new CodeWriter(sb);

        writer.WriteLine("name: {0}", snapshot.Title);

        writer.WriteLine("request:");
        writer.Indent();
        WriteOperationRequest(writer, request);
        writer.Unindent();

        writer.WriteLine("response: >-");
        writer.Indent();
        // TODO: Strip operation plan from response and render separately
        WriteOperationResult(writer, response);
        writer.Unindent();

        writer.WriteLine("sourceSchemas:");
        writer.Indent();

        foreach (var sourceSchema in gateway.SourceSchemas)
        {
            writer.WriteLine("- name: {0}", sourceSchema.Name);
            writer.Indent();
            writer.WriteLine("schema: >-");
            writer.Indent();
            WriteMultilineString(writer, sourceSchema.SourceText);
            writer.Unindent();

            var interactions = gateway.Interactions.GetValueOrDefault(sourceSchema.Name);

            if (interactions is not null)
            {
                writer.WriteLine("interactions:");
                writer.Indent();

                foreach (var (id, requestResponse) in interactions.OrderBy(x => x.Key))
                {
                    writer.WriteLine("- request: {0}", requestResponse.Request!);
                    writer.Indent();
                    writer.WriteLine("response: {0}", requestResponse.Response!);
                    writer.Unindent();
                }

                writer.Unindent();
            }

            writer.Unindent();
        }

        writer.Unindent();

        snapshot.Add(sb.ToString());

        snapshot.Match();
    }

    private static void WriteOperationRequest(CodeWriter writer, HotChocolate.Transport.OperationRequest request)
    {
        if (request.OnError is not null && request.OnError != ErrorHandlingMode.Propagate)
        {
            writer.WriteLine("onError: {0}", request.OnError);
        }

        writer.WriteLine("document: >-");
        writer.Indent();
        WriteMultilineString(writer, request.Query!);
        writer.Unindent();

        if (request.Variables is not null)
        {
            writer.WriteLine("variables: >-");
            writer.Indent();

            var jsonVariables = JsonSerializer.Serialize(
                request.Variables,
                new JsonSerializerOptions { WriteIndented = true });

            var reader = new StringReader(jsonVariables);
            var line = reader.ReadLine();
            while (line != null)
            {
                writer.WriteLine(line);
                line = reader.ReadLine();
            }

            writer.Unindent();
        }
    }

    private static void WriteOperationResult(CodeWriter writer, HotChocolate.Transport.OperationResult result)
    {
        var memoryStream = new MemoryStream();
        using var jsonWriter = new Utf8JsonWriter(memoryStream, new JsonWriterOptions { Indented = true });

        jsonWriter.WriteStartObject();

        if (result.RequestIndex.HasValue)
        {
            jsonWriter.WriteNumber("requestIndex", result.RequestIndex.Value);
        }

        if (result.VariableIndex.HasValue)
        {
            jsonWriter.WriteNumber("variableIndex", result.VariableIndex.Value);
        }

        if (result.Data.ValueKind is JsonValueKind.Object)
        {
            jsonWriter.WritePropertyName("data");
            result.Data.WriteTo(jsonWriter);
        }

        if (result.Errors.ValueKind is JsonValueKind.Array)
        {
            jsonWriter.WritePropertyName("errors");
            result.Errors.WriteTo(jsonWriter);
        }

        if (result.Extensions.ValueKind is JsonValueKind.Object)
        {
            jsonWriter.WritePropertyName("extensions");
            result.Extensions.WriteTo(jsonWriter);
        }

        jsonWriter.WriteEndObject();
        jsonWriter.Flush();

        memoryStream.Position = 0;

        var reader = new StreamReader(memoryStream);
        var line = reader.ReadLine();
        while (line != null)
        {
            writer.WriteLine(line);
            line = reader.ReadLine();
        }
    }

    private static void WriteMultilineString(CodeWriter writer, string multilineString)
    {
        var reader = new StringReader(multilineString);
        var line = reader.ReadLine();
        while (line != null)
        {
            writer.WriteLine(line);
            line = reader.ReadLine();
        }
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

    private static IServiceCollection AddHttpClient(
        IServiceCollection services,
        string name,
        TestServer server,
        SourceSchemaOptions options)
    {
        services.TryAddSingleton<IHttpClientFactory, Factory>();
        return services.AddSingleton(new TestServerRegistration(name, server, options));
    }

    public class Gateway(
        TestServer testServer,
        List<SourceSchemaText> sourceSchemas,
        ConcurrentDictionary<string, ConcurrentDictionary<int, RequestResponsePair>> interactions) : IDisposable
    {
        public HttpClient CreateClient() => testServer.CreateClient();

        public List<SourceSchemaText> SourceSchemas => sourceSchemas;

        public ConcurrentDictionary<string, ConcurrentDictionary<int, RequestResponsePair>> Interactions => interactions;

        public void Dispose()
        {
            testServer.Dispose();
        }
    }

    public class RequestResponsePair
    {
        public string? Request
        {
            get;
            set
            {
                if (field is not null)
                {
                    throw new InvalidOperationException();
                }

                field = value;
            }
        }

        public string? Response
        {
            get;
            set
            {
                if (field is not null)
                {
                    throw new InvalidOperationException();
                }

                field = value;
            }
        }
    }

    private sealed class RegisterFusionDirectivesTypeInterceptor : TypeInterceptor
    {
        private bool _registeredTypes;

        public override IEnumerable<TypeReference> RegisterMoreTypes(
            IReadOnlyCollection<ITypeDiscoveryContext> discoveryContexts)
        {
            if (!_registeredTypes)
            {
                var typeInspector = discoveryContexts.First().DescriptorContext.TypeInspector;

                yield return typeInspector.GetTypeRef(typeof(HotChocolate.Types.Composite.Lookup));
                yield return typeInspector.GetTypeRef(typeof(HotChocolate.Types.Composite.Internal));
                yield return typeInspector.GetTypeRef(typeof(HotChocolate.Types.Composite.EntityKey));
                yield return typeInspector.GetTypeRef(typeof(HotChocolate.Types.Composite.Is));

                _registeredTypes = true;
            }
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
