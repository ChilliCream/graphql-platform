using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using Moq;

namespace HotChocolate.Execution.Pipeline;

public class DocumentCacheMiddlewareTests
{
    [Fact]
    public async Task RetrieveItemFromCache_DocumentFoundOnCache()
    {
        // arrange
        var cache = new Caching.DefaultDocumentCache();
        var hashProvider = new MD5DocumentHashProvider();

        var middleware = DocumentCacheMiddleware.Create(
            _ => default,
            new NoopExecutionDiagnosticEvents(),
            cache,
            hashProvider);

        var request = OperationRequestBuilder.New()
            .SetDocument("{ a }")
            .SetDocumentId("a")
            .Build();

        var document = Utf8GraphQLParser.Parse("{ a }");
        cache.TryAddDocument("a", new CachedDocument(document, false));

        var requestContext = new Mock<IRequestContext>();
        var schema = new Mock<ISchema>();
        requestContext.SetupGet(t => t.Schema).Returns(schema.Object);
        requestContext.SetupGet(t => t.Request).Returns(request);
        requestContext.SetupProperty(t => t.DocumentId);
        requestContext.SetupProperty(t => t.Document);
        requestContext.SetupProperty(t => t.ValidationResult);
        schema.Setup(s => s.Name).Returns("SchemaName");

        // act
        await middleware.InvokeAsync(requestContext.Object);

        // assert
        Assert.Equal(document, requestContext.Object.Document);
        Assert.Equal("a", requestContext.Object.DocumentId);
    }

    [Fact]
    public async Task RetrieveItemFromCacheByHash_DocumentFoundOnCache()
    {
        // arrange
        var cache = new Caching.DefaultDocumentCache();
        var hashProvider = new MD5DocumentHashProvider();

        var middleware = DocumentCacheMiddleware.Create(
            _ => default,
            new NoopExecutionDiagnosticEvents(),
            cache,
            hashProvider);

        var request = OperationRequestBuilder.New()
            .SetDocument("{ a }")
            .SetDocumentHash("a")
            .Build();

        var document = Utf8GraphQLParser.Parse("{ a }");
        cache.TryAddDocument("a", new CachedDocument(document, false));

        var requestContext = new Mock<IRequestContext>();
        var schema = new Mock<ISchema>();
        requestContext.SetupGet(t => t.Schema).Returns(schema.Object);
        requestContext.SetupGet(t => t.Request).Returns(request);
        requestContext.SetupProperty(t => t.DocumentId);
        requestContext.SetupProperty(t => t.Document);
        requestContext.SetupProperty(t => t.ValidationResult);
        schema.Setup(s => s.Name).Returns("SchemaName");

        // act
        await middleware.InvokeAsync(requestContext.Object);

        // assert
        Assert.Equal(document, requestContext.Object.Document);
        Assert.Equal("a", requestContext.Object.DocumentId);
    }

    [Fact]
    public async Task RetrieveItemFromCache_DocumentNotFoundOnCache()
    {
        // arrange
        var cache = new Caching.DefaultDocumentCache();
        var hashProvider = new MD5DocumentHashProvider();

        var middleware = DocumentCacheMiddleware.Create(
            _ => default,
            new NoopExecutionDiagnosticEvents(),
            cache,
            hashProvider);

        var request = OperationRequestBuilder.New()
            .SetDocument("{ a }")
            .SetDocumentId("a")
            .Build();

        var document = Utf8GraphQLParser.Parse("{ a }");
        cache.TryAddDocument("b", new CachedDocument(document, false));

        var requestContext = new Mock<IRequestContext>();
        var schema = new Mock<ISchema>();
        requestContext.SetupGet(t => t.Schema).Returns(schema.Object);
        requestContext.SetupGet(t => t.Request).Returns(request);
        requestContext.SetupProperty(t => t.DocumentId);
        requestContext.SetupProperty(t => t.DocumentHash);
        requestContext.SetupProperty(t => t.Document);
        requestContext.SetupProperty(t => t.ValidationResult);
        schema.Setup(s => s.Name).Returns("SchemaName");

        // act
        await middleware.InvokeAsync(requestContext.Object);

        // assert
        Assert.Null(requestContext.Object.Document);
        Assert.Null(requestContext.Object.DocumentId);
        Assert.Equal("1_4JnW9GhGu3YdhGeMefaA", requestContext.Object.DocumentHash);
    }

    [Fact]
    public async Task AddItemToCacheWithDocumentId()
    {
        // arrange
        var cache = new Caching.DefaultDocumentCache();
        var hashProvider = new MD5DocumentHashProvider();

        var request = OperationRequestBuilder.New()
            .SetDocument("{ a }")
            .Build();

        var document = Utf8GraphQLParser.Parse("{ a }");

        var middleware = DocumentCacheMiddleware.Create(
            context =>
            {
                context.Document = document;
                context.DocumentId = "a";
                return default;
            },
            new NoopExecutionDiagnosticEvents(),
            cache,
            hashProvider);

        var requestContext = new Mock<IRequestContext>();
        requestContext.SetupGet(t => t.Request).Returns(request);
        requestContext.SetupProperty(t => t.DocumentId);
        requestContext.SetupProperty(t => t.DocumentHash);
        requestContext.SetupProperty(t => t.Document);
        requestContext.SetupProperty(t => t.ValidationResult);

        // act
        await middleware.InvokeAsync(requestContext.Object);

        // assert
        Assert.Equal(document, requestContext.Object.Document);
        Assert.Equal("a", requestContext.Object.DocumentId);
        Assert.Equal("1_4JnW9GhGu3YdhGeMefaA", requestContext.Object.DocumentHash);
    }

    [Fact]
    public async Task AddItemToCacheWithDocumentHash()
    {
        // arrange
        var cache = new Caching.DefaultDocumentCache();
        var hashProvider = new MD5DocumentHashProvider();

        var request = OperationRequestBuilder.New()
            .SetDocument("{ a }")
            .Build();

        var parserMiddleware = DocumentParserMiddleware.Create(
            _ => default,
            new NoopExecutionDiagnosticEvents(),
            hashProvider,
            new ParserOptions());

        var middleware = DocumentCacheMiddleware.Create(
            context => parserMiddleware.InvokeAsync(context),
            new NoopExecutionDiagnosticEvents(),
            cache,
            hashProvider);

        var requestContext = new Mock<IRequestContext>();
        requestContext.SetupGet(t => t.Request).Returns(request);
        requestContext.SetupProperty(t => t.DocumentId);
        requestContext.SetupProperty(t => t.DocumentHash);
        requestContext.SetupProperty(t => t.Document);
        requestContext.SetupProperty(t => t.ValidationResult);

        // act
        await middleware.InvokeAsync(requestContext.Object);

        // assert
        Assert.NotNull(requestContext.Object.Document);
        Assert.Equal(requestContext.Object.DocumentHash, requestContext.Object.DocumentId);
        Assert.Equal("1_4JnW9GhGu3YdhGeMefaA", requestContext.Object.DocumentHash);
    }
}
