using System.Text;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Options;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace HotChocolate.Execution.Pipeline;

public class ReadPersistedOperationMiddlewareTests
{
    [Fact]
    public async Task SetDocumentHash_On_Context_When_Algorithm_Matches()
    {
        // arrange
        var hashAlgorithm = new Sha256DocumentHashProvider();

        var documents = new Dictionary<OperationDocumentId, IOperationDocument>
        {
            {
                new OperationDocumentId("a"),
                new FakeDocumentWithHash(
                    "{ a }",
                    new OperationDocumentHash(
                        "abc",
                        hashAlgorithm.Name,
                        hashAlgorithm.Format))
            }
        };

        var documentStore = new FakeDocumentStore(documents);

        var services = new ServiceCollection();
        services.AddSingleton<IExecutionDiagnosticEvents, NoopExecutionDiagnosticEvents>();
        services.AddSingleton<IOperationDocumentStorage>(documentStore);
        services.AddSingleton<IDocumentHashProvider>(hashAlgorithm);

        var options = new RequestExecutorOptions
        {
            PersistedOperations = new PersistedOperationOptions
            {
                SkipPersistedDocumentValidation = true
            }
        };

        var factoryContext = new RequestCoreMiddlewareContext(
            "Default",
            services.BuildServiceProvider(),
            services.BuildServiceProvider(),
            options);

        var schema = CreateSchema();
        var errorHandler = new Mock<IErrorHandler>();
        var requestContext = new RequestContext(
            schema,
            1,
            errorHandler.Object,
            new NoopExecutionDiagnosticEvents());

        requestContext.Initialize(
            OperationRequestBuilder.New()
                .SetDocumentId("a")
                .Build(),
            services.BuildServiceProvider());

        var middleware = ReadPersistedOperationMiddleware.Create();
        var requestDelegate = middleware(factoryContext, async _ => await Task.CompletedTask);

        // act
        await requestDelegate(requestContext);

        // assert
        Assert.NotNull(requestContext.Document);
        Assert.Equal("abc", requestContext.DocumentHash);
    }

    [Fact]
    public async Task Do_Not_SetDocumentHash_On_Context_When_Algorithm_Does_Not_Match()
    {
        // arrange
        var hashAlgorithm = new Sha256DocumentHashProvider();

        var documents = new Dictionary<OperationDocumentId, IOperationDocument>
        {
            {
                new OperationDocumentId("a"),
                new FakeDocumentWithHash(
                    "{ a }",
                    new OperationDocumentHash(
                        "abc",
                        "abc",
                        hashAlgorithm.Format))
            }
        };

        var documentStore = new FakeDocumentStore(documents);

        var services = new ServiceCollection();
        services.AddSingleton<IExecutionDiagnosticEvents, NoopExecutionDiagnosticEvents>();
        services.AddSingleton<IOperationDocumentStorage>(documentStore);
        services.AddSingleton<IDocumentHashProvider>(hashAlgorithm);

        var options = new RequestExecutorOptions
        {
            PersistedOperations = new PersistedOperationOptions
            {
                SkipPersistedDocumentValidation = true
            }
        };

        var factoryContext = new RequestCoreMiddlewareContext(
            "Default",
            services.BuildServiceProvider(),
            services.BuildServiceProvider(),
            options);

        var schema = CreateSchema();
        var errorHandler = new Mock<IErrorHandler>();
        var requestContext = new RequestContext(
            schema,
            1,
            errorHandler.Object,
            new NoopExecutionDiagnosticEvents());

        requestContext.Initialize(
            OperationRequestBuilder.New()
                .SetDocumentId("a")
                .Build(),
            services.BuildServiceProvider());

        var middleware = ReadPersistedOperationMiddleware.Create();
        var requestDelegate = middleware(factoryContext, async _ => await Task.CompletedTask);

        // act
        await requestDelegate(requestContext);

        // assert
        Assert.NotNull(requestContext.Document);
        Assert.Null(requestContext.DocumentHash);
    }

    private static ISchema CreateSchema()
        => SchemaBuilder.New()
            .AddDocumentFromString("type Query { a: String }")
            .Use(_ => _)
            .Create();

    private sealed class FakeDocumentStore(
        Dictionary<OperationDocumentId, IOperationDocument> documents)
        : IOperationDocumentStorage
    {
        public ValueTask SaveAsync(
            OperationDocumentId documentId,
            IOperationDocument document,
            CancellationToken cancellationToken = default)
        {
            if (documents.ContainsKey(documentId))
            {
                documents[documentId] = document;
            }
            else
            {
                documents.Add(documentId, document);
            }

            return default;
        }

        public async ValueTask<IOperationDocument?> TryReadAsync(
            OperationDocumentId documentId,
            CancellationToken cancellationToken = default)
        {
            if (documents.TryGetValue(documentId, out var document))
            {
                return await new ValueTask<IOperationDocument?>(document);
            }

            return null;
        }
    }

    private sealed class FakeDocumentWithHash
        : IOperationDocument
        , IOperationDocumentHashProvider
    {
        public FakeDocumentWithHash(string document, OperationDocumentHash hash)
        {
            Document = document;
            Hash = hash;
        }

        public string Document { get; }

        public OperationDocumentHash Hash { get; }

        public ReadOnlySpan<byte> AsSpan()
            => Encoding.UTF8.GetBytes(Document).AsSpan();

        public byte[] ToArray()
            => Encoding.UTF8.GetBytes(Document);

        public Task WriteToAsync(
            Stream output,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public override string ToString() => Document;
    }
}
