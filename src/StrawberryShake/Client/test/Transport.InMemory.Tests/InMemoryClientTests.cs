using System.Text;
using HotChocolate;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace StrawberryShake.Transport.InMemory;

public class InMemoryClientTests
{
    [Fact]
    public void Constructor_AllArgs_NoException()
    {
        // arrange
        var name = "Foo";

        // act
        var ex = Record.Exception(() => new InMemoryClient(name));

        // assert
        Assert.Null(ex);
    }

    [Fact]
    public void Constructor_NoName_ThrowException()
    {
        // arrange
        string name = null!;

        // act
        var ex = Record.Exception(() => new InMemoryClient(name));

        // assert
        Assert.IsType<ArgumentException>(ex);
    }

    [Fact]
    public async Task ExecuteAsync_NoRequest_ThrowException()
    {
        // arrange
        var client = new InMemoryClient("Foo");

        // act
        var ex =
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
        var ex =
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
        var request = Assert.IsType<HotChocolate.Execution.OperationRequest>(executor.Request);
        Assert.Equal(operationRequest.Name, request.OperationName);
        Assert.Equal(variables, request.VariableValues);
        Assert.Equal("{ foo }", Encoding.UTF8.GetString(request.Document!.AsSpan()));
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
                    It.IsAny<OperationRequestBuilder>(),
                    It.IsAny<CancellationToken>()));

        // act
        await client.ExecuteAsync(operationRequest);

        // assert
        interceptorMock
            .Verify(x => x
                    .OnCreateAsync(
                        StubExecutor.ApplicationServiceProvider,
                        operationRequest,
                        It.IsAny<OperationRequestBuilder>(),
                        It.IsAny<CancellationToken>()),
                Times.Exactly(2));
    }

    private sealed class StubExecutor : IRequestExecutor
    {
        public IOperationRequest? Request { get; private set; }

        public ulong Version { get; }

        public Task<IExecutionResult> ExecuteAsync(
            IOperationRequest request,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult<IExecutionResult>(null!);
        }

        public Task<IResponseStream> ExecuteBatchAsync(
            OperationRequestBatch requestBatch,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public ISchema Schema => null!;

        public IServiceProvider Services { get; } =
            new ServiceCollection()
                .AddSingleton(ApplicationServiceProvider)
                .BuildServiceProvider();

        public static IApplicationServiceProvider ApplicationServiceProvider { get; } =
            new DefaultApplicationServiceProvider(
                new ServiceCollection()
                    .BuildServiceProvider());
    }

    private sealed class DefaultApplicationServiceProvider : IApplicationServiceProvider
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
