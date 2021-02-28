using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace StrawberryShake.Transport.InMemory
{
    public class InMemoryClientFactoryServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddInMemoryClient_NoServices_ThrowException()
        {
            // arrange
            IServiceCollection serviceCollection = default!;
            string name = "Foo";

            // act
            // act
            Exception? ex = Record.Exception(() => serviceCollection.AddInMemoryClient(name));

            // assert
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public void AddInMemoryClient_NoName_ThrowException()
        {
            // arrange
            IServiceCollection serviceCollection = new ServiceCollection();
            string name = null!;

            // act
            // act
            Exception? ex = Record.Exception(() => serviceCollection.AddInMemoryClient(name));

            // assert
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public async Task AddInMemoryClient_AllArgs_AddClient()
        {
            // arrange
            var collection = new ServiceCollection();
            collection
                .AddInMemoryClient("Foo")
                .ConfigureInMemoryClient(x => x.SchemaName = "Bar");
            IOptionsMonitor<InMemoryClientFactoryOptions> monitor = collection
                .BuildServiceProvider()
                .GetRequiredService<IOptionsMonitor<InMemoryClientFactoryOptions>>();
            var stubClient = new InMemoryClient("bar");

            // act
            await monitor.Get("Foo").InMemoryClientActions.Single()(stubClient, default);

            // assert
            Assert.Equal("Bar", stubClient.SchemaName.Value);
        }

        [Fact]
        public void AddInMemoryClientAction_NoServices_ThrowException()
        {
            // arrange
            IServiceCollection serviceCollection = default!;
            string name = "Foo";
            Action<IInMemoryClient> action = x => x.SchemaName = "Bar";

            // act
            Exception? ex = Record.Exception(() =>
                serviceCollection.AddInMemoryClient(name, action));

            // assert
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public void AddInMemoryClientAction_NoName_ThrowException()
        {
            // arrange
            IServiceCollection serviceCollection = new ServiceCollection();
            string name = null!;
            Action<IInMemoryClient> action = x => x.SchemaName = "Bar";

            // act
            Exception? ex = Record.Exception(() =>
                serviceCollection.AddInMemoryClient(name, action));

            // assert
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public void AddInMemoryClientAction_NoAction_ThrowException()
        {
            // arrange
            IServiceCollection serviceCollection = new ServiceCollection();
            string name = "Foo";
            Action<IInMemoryClient> action = null!;

            // act
            Exception? ex = Record.Exception(() =>
                serviceCollection.AddInMemoryClient(name, action));

            // assert
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public async Task AddInMemoryClientAction_AllArgs_AddClient()
        {
            // arrange
            var collection = new ServiceCollection();
            collection
                .AddInMemoryClient("Foo", x => x.SchemaName = "Bar");
            IOptionsMonitor<InMemoryClientFactoryOptions> monitor = collection
                .BuildServiceProvider()
                .GetRequiredService<IOptionsMonitor<InMemoryClientFactoryOptions>>();
            var stubClient = new InMemoryClient("bar");

            // act
            await monitor.Get("Foo").InMemoryClientActions.Single()(stubClient, default);

            // assert
            Assert.Equal("Bar", stubClient.SchemaName.Value);
        }


        [Fact]
        public void AddInMemoryClientActionServiceProvider_NoServices_ThrowException()
        {
            // arrange
            IServiceCollection serviceCollection = default!;
            string name = "Foo";
            Action<IServiceProvider, IInMemoryClient> action = (_, x) => x.SchemaName = "Bar";

            // act
            Exception? ex = Record.Exception(() =>
                serviceCollection.AddInMemoryClient(name, action));

            // assert
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public void AddInMemoryClientActionServiceProvider_NoName_ThrowException()
        {
            // arrange
            IServiceCollection serviceCollection = new ServiceCollection();
            string name = null!;
            Action<IServiceProvider, IInMemoryClient> action = (_, x) => x.SchemaName = "Bar";

            // act
            Exception? ex = Record.Exception(() =>
                serviceCollection.AddInMemoryClient(name, action));

            // assert
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public void AddInMemoryClientActionServiceProvider_NoAction_ThrowException()
        {
            // arrange
            IServiceCollection serviceCollection = new ServiceCollection();
            string name = "Foo";
            Action<IServiceProvider, IInMemoryClient> action = null!;

            // act
            Exception? ex = Record.Exception(() =>
                serviceCollection.AddInMemoryClient(name, action));

            // assert
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public async Task AddInMemoryClientActionServiceProvider_AllArgs_AddClient()
        {
            // arrange
            var collection = new ServiceCollection();
            collection
                .AddInMemoryClient("Foo", (_, x) => x.SchemaName = "Bar");
            IOptionsMonitor<InMemoryClientFactoryOptions> monitor = collection
                .BuildServiceProvider()
                .GetRequiredService<IOptionsMonitor<InMemoryClientFactoryOptions>>();
            var stubClient = new InMemoryClient("bar");

            // act
            await monitor.Get("Foo").InMemoryClientActions.Single()(stubClient, default);

            // assert
            Assert.Equal("Bar", stubClient.SchemaName.Value);
        }

        [Fact]
        public void AddInMemoryClientAsyncAction_NoServices_ThrowException()
        {
            // arrange
            IServiceCollection serviceCollection = default!;
            string name = "Foo";
            Func<IInMemoryClient, CancellationToken, ValueTask> action = (x, _) =>
            {
                x.SchemaName = "Bar";
                return default;
            };

            // act
            Exception? ex = Record.Exception(() =>
                serviceCollection.AddInMemoryClientAsync(name, action));

            // assert
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public void AddInMemoryClientAsyncAction_NoName_ThrowException()
        {
            // arrange
            IServiceCollection serviceCollection = new ServiceCollection();
            string name = null!;
            Func<IInMemoryClient, CancellationToken, ValueTask> action = (x, _) =>
            {
                x.SchemaName = "Bar";
                return default;
            };

            // act
            Exception? ex = Record.Exception(() =>
                serviceCollection.AddInMemoryClientAsync(name, action));

            // assert
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public void AddInMemoryClientAsyncAction_NoAction_ThrowException()
        {
            // arrange
            IServiceCollection serviceCollection = new ServiceCollection();
            string name = "Foo";
            Func<IInMemoryClient, CancellationToken, ValueTask> action = null!;

            // act
            Exception? ex = Record.Exception(() =>
                serviceCollection.AddInMemoryClientAsync(name, action));

            // assert
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public async Task AddInMemoryClientAsyncAction_AllArgs_AddClient()
        {
            // arrange
            var collection = new ServiceCollection();
            collection
                .AddInMemoryClientAsync("Foo",
                    (x, _) =>
                    {
                        x.SchemaName = "Bar";
                        return default;
                    });
            IOptionsMonitor<InMemoryClientFactoryOptions> monitor = collection
                .BuildServiceProvider()
                .GetRequiredService<IOptionsMonitor<InMemoryClientFactoryOptions>>();
            var stubClient = new InMemoryClient("bar");

            // act
            await monitor.Get("Foo").InMemoryClientActions.Single()(stubClient, default);

            // assert
            Assert.Equal("Bar", stubClient.SchemaName.Value);
        }


        [Fact]
        public void AddInMemoryClientAsyncActionServiceProvider_NoServices_ThrowException()
        {
            // arrange
            IServiceCollection serviceCollection = default!;
            string name = "Foo";
            Func<IServiceProvider, IInMemoryClient, CancellationToken, ValueTask> action =
                (_, x, _) =>
                {
                    x.SchemaName = "Bar";
                    return default;
                };

            // act
            Exception? ex = Record.Exception(() =>
                serviceCollection.AddInMemoryClientAsync(name, action));

            // assert
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public void AddInMemoryClientAsyncActionServiceProvider_NoName_ThrowException()
        {
            // arrange
            IServiceCollection serviceCollection = new ServiceCollection();
            string name = null!;
            Func<IServiceProvider, IInMemoryClient, CancellationToken, ValueTask> action =
                (_, x, _) =>
                {
                    x.SchemaName = "Bar";
                    return default;
                };

            // act
            Exception? ex = Record.Exception(() =>
                serviceCollection.AddInMemoryClientAsync(name, action));

            // assert
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public void AddInMemoryClientAsyncActionServiceProvider_NoAction_ThrowException()
        {
            // arrange
            IServiceCollection serviceCollection = new ServiceCollection();
            string name = "Foo";
            Func<IServiceProvider, IInMemoryClient, CancellationToken, ValueTask> action = null!;

            // act
            Exception? ex = Record.Exception(() =>
                serviceCollection.AddInMemoryClientAsync(name, action));

            // assert
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public async Task AddInMemoryClientAsyncActionServiceProvider_AllArgs_AddClient()
        {
            // arrange
            var collection = new ServiceCollection();
            collection
                .AddInMemoryClientAsync("Foo",
                    (_, x, _) =>
                    {
                        x.SchemaName = "Bar";
                        return default;
                    });
            IOptionsMonitor<InMemoryClientFactoryOptions> monitor = collection
                .BuildServiceProvider()
                .GetRequiredService<IOptionsMonitor<InMemoryClientFactoryOptions>>();
            var stubClient = new InMemoryClient("bar");

            // act
            await monitor.Get("Foo").InMemoryClientActions.Single()(stubClient, default);

            // assert
            Assert.Equal("Bar", stubClient.SchemaName.Value);
        }
    }
}
