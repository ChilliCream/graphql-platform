using System.Diagnostics.CodeAnalysis;
using System.Text;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types;
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
        var operationRequest = new OperationRequest("foo", new StubDocument(), new Dictionary<string, object?>());
        var executor = new StubExecutor();
        client.Executor = executor;
        client.RequestInterceptors.Add(interceptorMock.Object);
        client.RequestInterceptors.Add(interceptorMock.Object);
        interceptorMock
            .Setup(x => x
                .OnCreateAsync(
                    StubExecutor.RootServiceProvider,
                    operationRequest,
                    It.IsAny<OperationRequestBuilder>(),
                    It.IsAny<CancellationToken>()));

        // act
        await client.ExecuteAsync(operationRequest);

        // assert
        interceptorMock
            .Verify(x => x
                    .OnCreateAsync(
                        StubExecutor.RootServiceProvider,
                        operationRequest,
                        It.IsAny<OperationRequestBuilder>(),
                        It.IsAny<CancellationToken>()),
                Times.Exactly(2));
    }

    private sealed class StubExecutor : IRequestExecutor
    {
        public ISchemaDefinition Schema => new StubSchema(Services);

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

        public IServiceProvider Services { get; } =
            new ServiceCollection()
                .AddSingleton<IRootServiceProviderAccessor>(
                    new StubRootServiceProviderAccessor { ServiceProvider = RootServiceProvider })
                .BuildServiceProvider();

        public static IServiceProvider RootServiceProvider { get; } =
            new ServiceCollection().BuildServiceProvider();

        private class StubRootServiceProviderAccessor : IRootServiceProviderAccessor
        {
            public required IServiceProvider ServiceProvider { get; set; }
        }

        private class StubSchema(IServiceProvider services) : ISchemaDefinition
        {
            public string Name => null!;
            public string? Description  => null;
            public IReadOnlyDirectiveCollection Directives  => null!;
            public IFeatureCollection Features  => null!;
            public ISyntaxNode ToSyntaxNode() => null!;

            public IServiceProvider Services => services;
            public IObjectTypeDefinition QueryType  => null!;
            public IObjectTypeDefinition? MutationType => null;
            public IObjectTypeDefinition? SubscriptionType  => null;
            public IReadOnlyTypeDefinitionCollection Types  => null!;
            public IReadOnlyDirectiveDefinitionCollection DirectiveDefinitions  => null!;
            public IObjectTypeDefinition GetOperationType(OperationType operation)
            {
                throw new NotImplementedException();
            }

            public bool TryGetOperationType(OperationType operation, [NotNullWhen(true)] out IObjectTypeDefinition? type)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IObjectTypeDefinition> GetPossibleTypes(ITypeDefinition abstractType)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<INameProvider> GetAllDefinitions()
            {
                throw new NotImplementedException();
            }

            public override string ToString()
            {
                return Name;
            }
        }
    }

    public class StubDocument : IDocument
    {
        public OperationKind Kind => OperationKind.Query;

        public ReadOnlySpan<byte> Body => Encoding.UTF8.GetBytes("{ foo }");

        public DocumentHash Hash { get; } = new("MD5", "ABC");
    }
}
