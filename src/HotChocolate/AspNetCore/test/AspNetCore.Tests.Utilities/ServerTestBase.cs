using HotChocolate.AspNetCore.Extensions;
using HotChocolate.Execution;
using HotChocolate.StarWars;
using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit.Abstractions;

namespace HotChocolate.AspNetCore.Tests.Utilities;

public abstract class ServerTestBase(TestServerFactory serverFactory) : IClassFixture<TestServerFactory>
{
    protected TestServerFactory ServerFactory { get; } = serverFactory;

    protected Uri Url { get; } = new("http://localhost:5000/graphql");

    protected virtual TestServer CreateStarWarsServer(
        string pattern = "/graphql",
        Action<IServiceCollection>? configureServices = default,
        Action<GraphQLEndpointConventionBuilder>? configureConventions = default,
        ITestOutputHelper? output = null,
        bool requireOperationName = false,
        string? environment = null)
    {
        var mockHostEnvironment = new Mock<IHostEnvironment>();
        mockHostEnvironment.Setup(env => env.EnvironmentName).Returns(environment ?? Environments.Development);

        return ServerFactory.Create(
            services =>
            {
                services
                    .AddSingleton(mockHostEnvironment.Object)
                    .AddRouting()
                    .AddHttpResponseFormatter()
                    .AddGraphQLServer()
                    .AddStarWarsTypes()
                    .AddTypeExtension<QueryExtension>()
                    .AddTypeExtension<SubscriptionsExtensions>()
                    .AddType<Foo>()
                    .AddStarWarsRepositories()
                    .AddInMemorySubscriptions()
                    .UseInstrumentation()
                    .UseExceptions()
                    .UseTimeout()
                    .UseDocumentCache()
                    .UseReadPersistedOperation()
                    .UseAutomaticPersistedOperationNotFound()
                    .UseWritePersistedOperation()
                    .UseDocumentParser()
                    .UseDocumentValidation()
                    .UseCostAnalyzer()
                    .UseOperationCache()
                    .UseOperationResolver()
                    .UseOperationVariableCoercion()
                    .UseOperationExecution()
                    .ConfigureSchemaServices(
                        s => s.AddSingleton<IOperationDocumentStorage, TestOperationDocumentStorage>())
                    .ModifyOptions(
                        o =>
                        {
                            o.EnableDefer = true;
                            o.EnableStream = true;
                        })
                    .AddGraphQLServer("StarWars")
                    .AddStarWarsTypes()
                    .AddGraphQLServer("evict")
                    .AddQueryType(d => d.Name("Query"))
                    .AddTypeExtension<QueryExtension>()
                    .AddGraphQLServer("arguments")
                    .AddQueryType(
                        d =>
                        {
                            d
                                .Name("QueryRoot");

                            d
                                .Field("double_arg")
                                .Argument("d", t => t.Type<FloatType>())
                                .Type<FloatType>()
                                .Resolve(c => c.ArgumentValue<double?>("d"));

                            d
                                .Field("decimal_arg")
                                .Argument("d", t => t.Type<DecimalType>())
                                .Type<DecimalType>()
                                .Resolve(c => c.ArgumentValue<decimal?>("d"));
                        })
                    .AddGraphQLServer("upload")
                    .AddQueryType<UploadQuery>();

                if (output is not null)
                {
                    services
                        .AddGraphQL()
                        .AddDiagnosticEventListener(_ => new SubscriptionTestDiagnostics(output));
                }

                services
                    .AddGraphQLServer("notnull")
                    .AddQueryType(c =>
                    {
                        c.Name("Query");
                        c.Field("error")
                            .Type<NonNullType<StringType>>()
                            .Resolve(_ => Task.FromResult<object?>(null));
                    });

                configureServices?.Invoke(services);
            },
            app => app
                .UseWebSockets()
                .UseRouting()
                .UseEndpoints(
                    endpoints =>
                    {
                        endpoints.MapGraphQLPersistedOperations(requireOperationName: requireOperationName);

                        var builder = endpoints.MapGraphQL(pattern)
                            .WithOptions(new GraphQLServerOptions
                            {
                                EnableBatching = true,
                                AllowedGetOperations =
                                    AllowedGetOperations.Query | AllowedGetOperations.Subscription,
                            });

                        configureConventions?.Invoke(builder);
                        endpoints.MapGraphQL("/notnull", "notnull");
                        endpoints.MapGraphQL("/evict", "evict");
                        endpoints.MapGraphQL("/arguments", "arguments");
                        endpoints.MapGraphQL("/upload", "upload");
                        endpoints.MapGraphQL("/starwars", "StarWars");
                        endpoints.MapGraphQL("/test", "test");
                        endpoints.MapGraphQL("/batching").WithOptions(new GraphQLServerOptions
                        {
                            // with defaults
                            // EnableBatching = false
                        });
                    }));
    }

    protected virtual TestServer CreateServer(
        Action<IEndpointRouteBuilder>? configureConventions = default)
    {
        return ServerFactory.Create(
            services => services
                .AddRouting()
                .AddHttpResponseFormatter()
                .AddGraphQLServer()
                .AddStarWarsTypes()
                .AddTypeExtension<QueryExtension>()
                .AddTypeExtension<SubscriptionsExtensions>()
                .AddStarWarsRepositories()
                .ModifyOptions(
                    o =>
                    {
                        o.EnableDefer = true;
                        o.EnableStream = true;
                    }),
            app => app
                .UseWebSockets()
                .UseRouting()
                .UseEndpoints(endpoints => configureConventions?.Invoke(endpoints)));
    }

    [DirectiveType(DirectiveLocation.Subscription)]
    public class Foo
    {
        public required int Bar { get; set; }
    }
}
