using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class IntegrationTests
{
    [Fact]
    public async Task Schema()
    {
        // arrange
        var services = CreateApplicationServices();

        // act
        var executor = await services.GetRequiredService<IRequestExecutorResolver>().GetRequestExecutorAsync();

        // assert
        executor.Schema.MatchSnapshot();
    }

    [Fact]
    public async Task Query_Books_And_Authors()
    {
        // arrange
        var services = CreateApplicationServices();
        var executor = await services.GetRequiredService<IRequestExecutorResolver>().GetRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                books {
                    nodes {
                        id
                        title
                        genre
                        author {
                            id
                            name
                        }
                    }
                }
            }
            """);

        // assert
        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Class_Request_Middleware()
    {
        // arrange
        var services = CreateApplicationServices(
            services =>
            {
                services
                    .AddSingleton<Service1>()
                    .AddSingleton<Service2>()
                    .AddScoped<Service3>();

                services
                    .AddGraphQLServer()
                    .UseRequest<SomeRequestMiddleware>()
                    .UseDefaultPipeline();
            });

        var executor = await services.GetRequiredService<IRequestExecutorResolver>().GetRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                books {
                    nodes {
                        id
                        title
                        genre
                        author {
                            id
                            name
                        }
                    }
                }
            }
            """);

        // assert
        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Query_Extension_Node_Resolver()
    {
        // arrange
        var services = CreateApplicationServices();
        var executor = await services.GetRequiredService<IRequestExecutorResolver>().GetRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                node(id: "QXV0aG9yOjE=") {
                    __typename
                }
            }
            """);

        // assert
        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Inherit_Interface_Fields()
    {
        // arrange
        var services = CreateApplicationServices();
        var executor = await services.GetRequiredService<IRequestExecutorResolver>().GetRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                books {
                    nodes {
                        idString
                    }
                }
            }
            """);

        // assert
        result.MatchMarkdownSnapshot();
    }

    private static IServiceProvider CreateApplicationServices(
        Action<IServiceCollection>? configure = null)
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection
            .AddSingleton<AuthorRepository>()
            .AddScoped<AuthorAddressRepository>()
            .AddSingleton<BookRepository>()
            .AddSingleton<ChapterRepository>();

        serviceCollection
            .AddGraphQLServer(disableDefaultSecurity: true)
            .AddCustomModule()
            .AddGlobalObjectIdentification()
            .AddMutationConventions();

        configure?.Invoke(serviceCollection);

        return serviceCollection.BuildServiceProvider();
    }
}
