using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace StrawberryShake.Transport.InMemory
{
    public class InMemoryClientTests
    {
        [Fact]
        public void Constructor_AllArgs_NoException()
        {
            // arrange
            string name = "Foo";

            // act
            Exception? ex = Record.Exception(() => new InMemoryClient(name));

            // assert
            Assert.Null(ex);
        }

        [Fact]
        public void Constructor_NoName_ThrowException()
        {
            // arrange
            string name = null!;

            // act
            Exception? ex = Record.Exception(() => new InMemoryClient(name));

            // assert
            Assert.IsType<ArgumentException>(ex);
        }

        [Fact]
        public async Task ExecuteAsync_NoRequest_ThrowException()
        {
            // arrange
            var client = new InMemoryClient("Foo");

            // act
            Exception? ex =
                await Record.ExceptionAsync(async () => await client.ExecuteAsync(null!));

            // assert
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public async Task ExecuteAsync_NoExecutor_ThrowException()
        {
            // arrange
            var client = new InMemoryClient("Foo");
            var operationRequest =
                new OperationRequest("foo", new StubDocument());

            // act
            Exception? ex =
                await Record.ExceptionAsync(async () =>
                    await client.ExecuteAsync(operationRequest));

            // assert
            Assert.IsType<GraphQLClientException>(ex);
        }

        [Fact]
        public async Task ExecuteAsync_Default_ExecuteQuery()
        {
            // arrange
            var client = new InMemoryClient("Foo");
            var variables = new Dictionary<string, object?>();
            var operationRequest = new OperationRequest("foo", new StubDocument(), variables);
            var executor = new StubExecutor();
            client.Executor = executor;

            // act
            await client.ExecuteAsync(operationRequest);

            // assert
            Assert.Equal(operationRequest.Name, executor.Request.OperationName);
            Assert.Equal(variables, executor.Request.VariableValues);
            Assert.Equal("{ foo }", Encoding.UTF8.GetString(executor.Request.Query!.AsSpan()));
        }

        [Fact]
        public async Task ExecuteAsync_Default_CallInterceptor()
        {
            // arrange
            var interceptorMock = new Mock<IInMemoryRequestInterceptor>();
            var client = new InMemoryClient("Foo");
            var variables = new Dictionary<string, object?>();
            var operationRequest = new OperationRequest("foo", new StubDocument(), variables);
            var executor = new StubExecutor();
            client.Executor = executor;
            client.RequestInterceptors.Add(interceptorMock.Object);
            client.RequestInterceptors.Add(interceptorMock.Object);
            interceptorMock
                .Setup(x => x
                    .OnCreateAsync(
                        StubExecutor.ApplicationServiceProvider,
                        operationRequest,
                        It.IsAny<IQueryRequestBuilder>(),
                        It.IsAny<CancellationToken>()));

            // act
            await client.ExecuteAsync(operationRequest);

            // assert
            interceptorMock
                .Verify(x => x
                        .OnCreateAsync(
                            StubExecutor.ApplicationServiceProvider,
                            operationRequest,
                            It.IsAny<IQueryRequestBuilder>(),
                            It.IsAny<CancellationToken>()),
                    Times.Exactly(2));
        }

        private class StubExecutor : IRequestExecutor
        {
            public IQueryRequest Request { get; private set; }

            public ulong Version { get; }

            public Task<IExecutionResult> ExecuteAsync(
                IQueryRequest request,
                CancellationToken cancellationToken = default)
            {
                Request = request;
                return Task.FromResult<IExecutionResult>(null!);
            }

            public Task<IBatchQueryResult> ExecuteBatchAsync(
                IEnumerable<IQueryRequest> requestBatch,
                bool allowParallelExecution = false,
                CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            public ISchema Schema { get; } = null!;

            public IServiceProvider Services { get; } = new ServiceCollection()
                .AddSingleton(ApplicationServiceProvider)
                .BuildServiceProvider();

            public static IApplicationServiceProvider ApplicationServiceProvider { get; } =
                new DefaultApplicationServiceProvider(
                    new ServiceCollection().BuildServiceProvider());
        }

        private class DefaultApplicationServiceProvider
            : IApplicationServiceProvider
        {
            private readonly IServiceProvider _applicationServices;

            public DefaultApplicationServiceProvider(IServiceProvider applicationServices)
            {
                _applicationServices = applicationServices ??
                    throw new ArgumentNullException(nameof(applicationServices));
            }

            public object? GetService(Type serviceType) =>
                _applicationServices.GetService(serviceType);
        }

        public class StubDocument : IDocument
        {
            public OperationKind Kind => OperationKind.Query;

            public ReadOnlySpan<byte> Body => Encoding.UTF8.GetBytes("{ foo }");

            public DocumentHash Hash { get; } = new("MD5", "ABC");
        }
    }
}
