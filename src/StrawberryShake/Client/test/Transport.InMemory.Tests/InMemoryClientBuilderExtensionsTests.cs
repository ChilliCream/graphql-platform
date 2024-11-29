using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace StrawberryShake.Transport.InMemory;

public class InMemoryClientBuilderExtensionsTests
{
    [Fact]
    public void ConfigureInMemoryClient_NoServices_ThrowException()
    {
        // arrange
        IInMemoryClientBuilder builder = default!;
        Action<IInMemoryClient> action = x => x.SchemaName = "Bar";

        // act
        var ex = Record.Exception(() => builder.ConfigureInMemoryClient(action));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public void ConfigureInMemoryClient_NoName_ThrowException()
    {
        // arrange
        var collection = new ServiceCollection();
        IInMemoryClientBuilder builder =
            new DefaultInMemoryClientBuilder(collection, "foo");
        Action<IInMemoryClient> action = null!;

        // act
        var ex = Record.Exception(() => builder.ConfigureInMemoryClient(action));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task ConfigureInMemoryClient_AllArgs_ConfigureClient()
    {
        // arrange
        var collection = new ServiceCollection();
        IInMemoryClientBuilder builder =
            new DefaultInMemoryClientBuilder(collection, "foo");
        Action<IInMemoryClient> action = x => x.SchemaName = "Bar";

        // act
        builder.ConfigureInMemoryClient(action);
        var stubClient = new InMemoryClient("bar");

        // assert
        await collection.BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<InMemoryClientFactoryOptions>>()
            .Get("foo")
            .InMemoryClientActions
            .Single()(stubClient, default);

        Assert.Equal("Bar", stubClient.SchemaName);
    }

    [Fact]
    public void ConfigureInMemoryClientAsync_NoServices_ThrowException()
    {
        // arrange
        IInMemoryClientBuilder builder = default!;
        Func<IInMemoryClient, CancellationToken, ValueTask> action = (x, _) =>
        {
            x.SchemaName = "Bar";
            return default;
        };

        // act
        var ex = Record.Exception(() => builder.ConfigureInMemoryClientAsync(action));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public void ConfigureInMemoryClientAsync_NoName_ThrowException()
    {
        // arrange
        var collection = new ServiceCollection();
        IInMemoryClientBuilder builder =
            new DefaultInMemoryClientBuilder(collection, "foo");
        Func<IInMemoryClient, CancellationToken, ValueTask> action = null!;

        // act
        var ex = Record.Exception(() => builder.ConfigureInMemoryClientAsync(action));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task ConfigureInMemoryClientAsync_AllArgs_ConfigureClient()
    {
        // arrange
        var collection = new ServiceCollection();
        IInMemoryClientBuilder builder =
            new DefaultInMemoryClientBuilder(collection, "foo");
        Func<IInMemoryClient, CancellationToken, ValueTask> action = (x, _) =>
        {
            x.SchemaName = "Bar";
            return default;
        };

        // act
        builder.ConfigureInMemoryClientAsync(action);
        var stubClient = new InMemoryClient("bar");

        // assert
        await collection.BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<InMemoryClientFactoryOptions>>()
            .Get("foo")
            .InMemoryClientActions
            .Single()(stubClient, default);

        Assert.Equal("Bar", stubClient.SchemaName);
    }

    [Fact]
    public void ConfigureInMemoryClientService_NoServices_ThrowException()
    {
        // arrange
        IInMemoryClientBuilder builder = default!;
        Action<IServiceProvider, IInMemoryClient> action = (_, x) => x.SchemaName = "Bar";

        // act
        var ex = Record.Exception(() => builder.ConfigureInMemoryClient(action));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public void ConfigureInMemoryClientService_NoName_ThrowException()
    {
        // arrange
        var collection = new ServiceCollection();
        IInMemoryClientBuilder builder =
            new DefaultInMemoryClientBuilder(collection, "foo");
        Action<IServiceProvider, IInMemoryClient> action = null!;

        // act
        var ex = Record.Exception(() => builder.ConfigureInMemoryClient(action));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task ConfigureInMemoryClientService_AllArgs_ConfigureClient()
    {
        // arrange
        var collection = new ServiceCollection();
        IInMemoryClientBuilder builder =
            new DefaultInMemoryClientBuilder(collection, "foo");
        Action<IServiceProvider, IInMemoryClient> action = (_, x) => x.SchemaName = "Bar";

        // act
        builder.ConfigureInMemoryClient(action);
        var stubClient = new InMemoryClient("bar");

        // assert
        await collection
            .AddOptions()
            .BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<InMemoryClientFactoryOptions>>()
            .Get("foo")
            .InMemoryClientActions
            .Single()(stubClient, default);

        Assert.Equal("Bar", stubClient.SchemaName);
    }

    [Fact]
    public void ConfigureInMemoryClientAsyncServiceProvider_NoServices_ThrowException()
    {
        // arrange
        IInMemoryClientBuilder builder = default!;
        Func<IServiceProvider, IInMemoryClient, CancellationToken, ValueTask> action =
            (_, x, _) =>
            {
                x.SchemaName = "Bar";
                return default;
            };

        // act
        var ex = Record.Exception(() => builder.ConfigureInMemoryClientAsync(action));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public void ConfigureInMemoryClientAsyncServiceProvider_NoName_ThrowException()
    {
        // arrange
        var collection = new ServiceCollection();
        IInMemoryClientBuilder builder =
            new DefaultInMemoryClientBuilder(collection, "foo");
        Func<IServiceProvider, IInMemoryClient, CancellationToken, ValueTask> action =
            null!;

        // act
        var ex = Record.Exception(() => builder.ConfigureInMemoryClientAsync(action));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task ConfigureInMemoryClientAsyncServiceProvider_AllArgs_ConfigureClient()
    {
        // arrange
        var collection = new ServiceCollection();
        IInMemoryClientBuilder builder =
            new DefaultInMemoryClientBuilder(collection, "foo");
        Func<IServiceProvider, IInMemoryClient, CancellationToken, ValueTask> action =
            (_, x, _) =>
            {
                x.SchemaName = "Bar";
                return default;
            };

        // act
        builder.ConfigureInMemoryClientAsync(action);
        var stubClient = new InMemoryClient("bar");

        // assert
        await collection
            .AddOptions()
            .BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<InMemoryClientFactoryOptions>>()
            .Get("foo")
            .InMemoryClientActions
            .Single()(stubClient, default);

        Assert.Equal("Bar", stubClient.SchemaName);
    }

    [Fact]
    public void ConfigureRequestInterceptorInstance_NoServices_ThrowException()
    {
        // arrange
        IInMemoryClientBuilder builder = default!;
        IInMemoryRequestInterceptor interceptor = new StubInterceptor();

        // act
        var ex =
            Record.Exception(() => builder.ConfigureRequestInterceptor(interceptor));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public void ConfigureRequestInterceptorInstance_NoName_ThrowException()
    {
        // arrange
        var collection = new ServiceCollection();
        IInMemoryClientBuilder builder =
            new DefaultInMemoryClientBuilder(collection, "foo");
        IInMemoryRequestInterceptor interceptor = null!;

        // act
        var ex =
            Record.Exception(() => builder.ConfigureRequestInterceptor(interceptor));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task ConfigureRequestInterceptorInstance_AllArgs_ConfigureClient()
    {
        // arrange
        var collection = new ServiceCollection();
        IInMemoryClientBuilder builder =
            new DefaultInMemoryClientBuilder(collection, "foo");
        IInMemoryRequestInterceptor interceptor = new StubInterceptor();

        // act
        builder.ConfigureRequestInterceptor(interceptor);
        var stubClient = new InMemoryClient("bar");

        // assert
        await collection
            .AddOptions()
            .BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<InMemoryClientFactoryOptions>>()
            .Get("foo")
            .InMemoryClientActions
            .Single()(stubClient, default);

        Assert.Equal(interceptor, stubClient.RequestInterceptors.FirstOrDefault());
    }

    [Fact]
    public void ConfigureRequestInterceptorGeneric_NoServices_ThrowException()
    {
        // arrange
        IInMemoryClientBuilder builder = default!;

        // act
        var ex =
            Record.Exception(() => builder.ConfigureRequestInterceptor<StubInterceptor>());

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task ConfigureRequestInterceptorGeneric_AllArgs_ConfigureClient()
    {
        // arrange
        var collection = new ServiceCollection();
        IInMemoryClientBuilder builder =
            new DefaultInMemoryClientBuilder(collection, "foo");
        var interceptor = new StubInterceptor();
        collection.AddSingleton(interceptor);

        // act
        builder.ConfigureRequestInterceptor<StubInterceptor>();
        var stubClient = new InMemoryClient("bar");

        // assert
        await collection
            .AddOptions()
            .BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<InMemoryClientFactoryOptions>>()
            .Get("foo")
            .InMemoryClientActions
            .Single()(stubClient, default);

        Assert.Equal(interceptor, stubClient.RequestInterceptors.FirstOrDefault());
    }

    [Fact]
    public void ConfigureRequestInterceptorFactory_NoBuilder_ThrowException()
    {
        // arrange
        IInMemoryRequestInterceptor interceptor = new StubInterceptor();
        IInMemoryClientBuilder builder = default!;
        Func<IServiceProvider, IInMemoryRequestInterceptor> factory = provider => interceptor;

        // act
        var ex =
            Record.Exception(() => builder.ConfigureRequestInterceptor(factory));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public void ConfigureRequestInterceptorFactory_NoFactory_ThrowException()
    {
        // arrange
        var collection = new ServiceCollection();
        IInMemoryClientBuilder builder =
            new DefaultInMemoryClientBuilder(collection, "foo");
        Func<IServiceProvider, IInMemoryRequestInterceptor> factory = null!;

        // act
        var ex =
            Record.Exception(() => builder.ConfigureRequestInterceptor(factory));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task ConfigureRequestInterceptorFactory_AllArgs_ConfigureClient()
    {
        // arrange
        var collection = new ServiceCollection();
        IInMemoryClientBuilder builder =
            new DefaultInMemoryClientBuilder(collection, "foo");
        IInMemoryRequestInterceptor interceptor = new StubInterceptor();

        // act
        builder.ConfigureRequestInterceptor(_ => interceptor);
        var stubClient = new InMemoryClient("bar");

        // assert
        await collection
            .AddOptions()
            .BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<InMemoryClientFactoryOptions>>()
            .Get("foo")
            .InMemoryClientActions
            .Single()(stubClient, default);

        Assert.Equal(interceptor, stubClient.RequestInterceptors.FirstOrDefault());
    }

    public class StubInterceptor : IInMemoryRequestInterceptor
    {
        public ValueTask OnCreateAsync(
            IServiceProvider serviceProvider,
            OperationRequest request,
            OperationRequestBuilder requestBuilder,
            CancellationToken cancellationToken)
        {
            return default;
        }
    }
}
