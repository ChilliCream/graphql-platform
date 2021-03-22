using System;
using System.ComponentModel.Design;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Moq;
using Xunit;

namespace StrawberryShake.Transport.InMemory
{
    public class DefaultInMemoryClientBuilderTests
    {
        [Fact]
        public void Constructor_AllArgs_NoException()
        {
            // arrange
            IRequestExecutorResolver executorResolver =
                new Mock<IRequestExecutorResolver>().Object;
            IOptionsMonitor<InMemoryClientFactoryOptions> optionsMonitor = new ServiceCollection()
                .Configure<InMemoryClientFactoryOptions>(_ => { })
                .BuildServiceProvider()
                .GetRequiredService<IOptionsMonitor<InMemoryClientFactoryOptions>>();

            // act
            Exception? ex = Record.Exception(() =>
                new DefaultInMemoryClientFactory(executorResolver, optionsMonitor));

            // assert
            Assert.Null(ex);
        }

        [Fact]
        public void Constructor_NoExecutor_ThrowException()
        {
            // arrange
            IRequestExecutorResolver executorResolver = default!;
            IOptionsMonitor<InMemoryClientFactoryOptions> optionsMonitor = new ServiceCollection()
                .Configure<InMemoryClientFactoryOptions>(_ => { })
                .BuildServiceProvider()
                .GetRequiredService<IOptionsMonitor<InMemoryClientFactoryOptions>>();

            // act
            Exception? ex = Record.Exception(() =>
                new DefaultInMemoryClientFactory(executorResolver, optionsMonitor));

            // assert
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public void Constructor_NoOptions_ThrowException()
        {
            // arrange
            IRequestExecutorResolver executorResolver =
                new Mock<IRequestExecutorResolver>().Object;
            IOptionsMonitor<InMemoryClientFactoryOptions> optionsMonitor = default!;

            // act
            Exception? ex = Record.Exception(() =>
                new DefaultInMemoryClientFactory(executorResolver, optionsMonitor));

            // assert
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public async Task CreateClientAsync_OptionsSet_CallConfigureClient()
        {
            // arrange
            bool wasCalled = false;
            IRequestExecutorResolver executorResolver =
                new Mock<IRequestExecutorResolver>().Object;
            IOptionsMonitor<InMemoryClientFactoryOptions> optionsMonitor = new ServiceCollection()
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
            IRequestExecutorResolver executorResolver =
                new Mock<IRequestExecutorResolver>().Object;
            IOptionsMonitor<InMemoryClientFactoryOptions> optionsMonitor =
                new ServiceCollection()
                    .AddOptions()
                    .BuildServiceProvider()
                    .GetRequiredService<IOptionsMonitor<InMemoryClientFactoryOptions>>();
            var factory = new DefaultInMemoryClientFactory(executorResolver, optionsMonitor);

            // act
            IInMemoryClient client = await factory.CreateAsync("Foo");

            // assert
            Assert.NotNull(client);
        }

        [Fact]
        public async Task CreateClientAsync_SchemaNameSet_CreateExecutorForSchema()
        {
            // arrange
            var nameString = new NameString("FooBar");
            IRequestExecutor executor = new Mock<IRequestExecutor>().Object;
            Mock<IRequestExecutorResolver> executorResolverMock = new();
            IRequestExecutorResolver executorResolver = executorResolverMock.Object;
            IOptionsMonitor<InMemoryClientFactoryOptions> optionsMonitor =
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
            IInMemoryClient client = await factory.CreateAsync("Foo");

            // assert
            Assert.Equal(client.Executor, executor);
        }

        [Fact]
        public async Task CreateClientAsync_ExecutorSet_SchemaNameOfExecutor()
        {
            // arrange
            var nameString = new NameString("FooBar");
            Mock<IRequestExecutor> executorMock = new();
            Mock<ISchema> schemaMock = new();
            Mock<IRequestExecutorResolver> executorResolverMock = new();
            IRequestExecutorResolver executorResolver = executorResolverMock.Object;
            IOptionsMonitor<InMemoryClientFactoryOptions> optionsMonitor =
                new ServiceCollection()
                    .Configure<InMemoryClientFactoryOptions>("Foo",
                        x => x.InMemoryClientActions.Add((memoryClient, token) =>
                        {
                            memoryClient.Executor = executorMock.Object;
                            return default;
                        }))
                    .BuildServiceProvider()
                    .GetRequiredService<IOptionsMonitor<InMemoryClientFactoryOptions>>();
            var factory = new DefaultInMemoryClientFactory(executorResolver, optionsMonitor);

            schemaMock.Setup(x => x.Name).Returns(nameString);
            executorMock.Setup(x => x.Schema).Returns(schemaMock.Object);

            // act
            IInMemoryClient client = await factory.CreateAsync("Foo");

            // assert
            Assert.Equal(client.SchemaName, nameString);
        }
    }
}
