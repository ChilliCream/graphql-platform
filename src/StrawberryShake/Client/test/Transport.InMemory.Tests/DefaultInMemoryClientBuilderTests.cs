using HotChocolate;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace StrawberryShake.Transport.InMemory;

public class DefaultInMemoryClientBuilderTests
{
    [Fact]
    public void Constructor_AllArgs_NoException()
    {
        // arrange
        var executorResolver = new Mock<IRequestExecutorResolver>().Object;
        var optionsMonitor = new ServiceCollection()
            .Configure<InMemoryClientFactoryOptions>(_ => { })
            .BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<InMemoryClientFactoryOptions>>();

        // act
        var ex = Record.Exception(() =>
            new DefaultInMemoryClientFactory(executorResolver, optionsMonitor));

        // assert
        Assert.Null(ex);
    }

    [Fact]
    public void Constructor_NoExecutor_ThrowException()
    {
        // arrange
        IRequestExecutorResolver executorResolver = default!;
        var optionsMonitor = new ServiceCollection()
            .Configure<InMemoryClientFactoryOptions>(_ => { })
            .BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<InMemoryClientFactoryOptions>>();

        // act
        var ex = Record.Exception(() =>
            new DefaultInMemoryClientFactory(executorResolver, optionsMonitor));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public void Constructor_NoOptions_ThrowException()
    {
        // arrange
        var executorResolver =
            new Mock<IRequestExecutorResolver>().Object;
        IOptionsMonitor<InMemoryClientFactoryOptions> optionsMonitor = default!;

        // act
        var ex = Record.Exception(() =>
            new DefaultInMemoryClientFactory(executorResolver, optionsMonitor));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task CreateClientAsync_OptionsSet_CallConfigureClient()
    {
        // arrange
        var wasCalled = false;
        var executorResolver =
            new Mock<IRequestExecutorResolver>().Object;
        var optionsMonitor = new ServiceCollection()
            .Configure<InMemoryClientFactoryOptions>("Foo", _ => { wasCalled = true; })
            .BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<InMemoryClientFactoryOptions>>();
        var factory = new DefaultInMemoryClientFactory(executorResolver, optionsMonitor);

        // act
        await factory.CreateAsync("Foo");

        // assert
        Assert.True(wasCalled);
    }

    [Fact]
    public async Task CreateClientAsync_NoOptionsSet_CreateClient()
    {
        // arrange
        var executorResolver =
            new Mock<IRequestExecutorResolver>().Object;
        var optionsMonitor =
            new ServiceCollection()
                .AddOptions()
                .BuildServiceProvider()
                .GetRequiredService<IOptionsMonitor<InMemoryClientFactoryOptions>>();
        var factory = new DefaultInMemoryClientFactory(executorResolver, optionsMonitor);

        // act
        var client = await factory.CreateAsync("Foo");

        // assert
        Assert.NotNull(client);
    }

    [Fact]
    public async Task CreateClientAsync_SchemaNameSet_CreateExecutorForSchema()
    {
        // arrange
        var nameString = "FooBar";
        var executor = new Mock<IRequestExecutor>().Object;
        Mock<IRequestExecutorResolver> executorResolverMock = new();
        var executorResolver = executorResolverMock.Object;
        var optionsMonitor =
            new ServiceCollection()
                .Configure<InMemoryClientFactoryOptions>("Foo",
                    x => x.InMemoryClientActions.Add((memoryClient, token) =>
                    {
                        memoryClient.SchemaName = nameString;
                        return default;
                    }))
                .BuildServiceProvider()
                .GetRequiredService<IOptionsMonitor<InMemoryClientFactoryOptions>>();
        var factory = new DefaultInMemoryClientFactory(executorResolver, optionsMonitor);

        executorResolverMock
            .Setup(
                x => x.GetRequestExecutorAsync(nameString, It.IsAny<CancellationToken>()))
            .ReturnsAsync(executor);

        // act
        var client = await factory.CreateAsync("Foo");

        // assert
        Assert.Equal(client.Executor, executor);
    }

    [Fact]
    public async Task CreateClientAsync_ExecutorSet_SchemaNameOfExecutor()
    {
        // arrange
        const string nameString = "FooBar";
        var executorProvider = CreateExecutorProvider(nameString);
        var executor = await executorProvider.GetRequestExecutorAsync(nameString);

        var optionsMonitor =
            new ServiceCollection()
                .Configure<InMemoryClientFactoryOptions>(
                    "Foo",
                    options => options.InMemoryClientActions.Add((client, _) =>
                    {
                        client.Executor = executor;
                        return default;
                    }))
                .BuildServiceProvider()
                .GetRequiredService<IOptionsMonitor<InMemoryClientFactoryOptions>>();

        var factory = new DefaultInMemoryClientFactory(executorProvider, optionsMonitor);

        // act
        var client = await factory.CreateAsync("Foo");

        // assert
        Assert.Equal(nameString, client.SchemaName);
    }

    private static IRequestExecutorResolver CreateExecutorProvider(string schemaName)
    {
        return new ServiceCollection()
            .AddGraphQLServer(schemaName)
            .AddDocumentFromString("type Query { hello: String }")
            .UseField(next => next)
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorResolver>();
    }
}
