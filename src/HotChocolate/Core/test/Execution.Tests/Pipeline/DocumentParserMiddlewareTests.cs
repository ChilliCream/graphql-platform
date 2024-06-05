using HotChocolate.Execution.Errors;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Options;
using HotChocolate.Language;
using Moq;

namespace HotChocolate.Execution.Pipeline;

public class DocumentParserMiddlewareTests
{
    [Fact]
    public async Task DocumentExists_SkipParsing_DocumentIsUnchanged()
    {
        // arrange
        var hashProvider = new MD5DocumentHashProvider();

        var middleware = DocumentParserMiddleware.Create(
            _ => default,
            new NoopExecutionDiagnosticEvents(),
            hashProvider,
            new ParserOptions());

        var request = OperationRequestBuilder.Create()
            .SetDocument("{ a }")
            .SetDocumentId("a")
            .Build();

        var document = Utf8GraphQLParser.Parse("{ a }");

        var requestContext = new Mock<IRequestContext>();
        requestContext.SetupGet(t => t.Request).Returns(request);
        requestContext.SetupProperty(t => t.Document, document);

        // act
        await middleware.InvokeAsync(requestContext.Object);

        // assert
        Assert.Equal(document, requestContext.Object.Document);
    }

    [Fact]
    public async Task NoDocument_ParseQuery_DocumentParsedAndHashed()
    {
        // arrange
        var hashProvider = new MD5DocumentHashProvider();

        var middleware = DocumentParserMiddleware.Create(
            _ => default,
            new NoopExecutionDiagnosticEvents(),
            hashProvider,
            new ParserOptions());

        var request = OperationRequestBuilder.Create()
            .SetDocument("{ a }")
            .SetDocumentId("a")
            .Build();

        var requestContext = new Mock<IRequestContext>();
        requestContext.SetupGet(t => t.Request).Returns(request);
        requestContext.SetupProperty(t => t.Document);
        requestContext.SetupProperty(t => t.DocumentId);

        // act
        await middleware.InvokeAsync(requestContext.Object);

        // assert
        Assert.NotNull(requestContext.Object.Document);
        Assert.NotNull(requestContext.Object.DocumentId);
    }

    [Fact]
    public async Task InvalidQuery_SyntaxError_ContextHasErrorResult()
    {
        // arrange
        var hashProvider = new MD5DocumentHashProvider();

        var middleware = DocumentParserMiddleware.Create(
            _ => throw new Exception("Should not be invoked."),
            new NoopExecutionDiagnosticEvents(),
            hashProvider,
            new ParserOptions());

        var request = OperationRequestBuilder.Create()
            .SetDocument("{")
            .SetDocumentId("a")
            .Build();

        var requestContext = new Mock<IRequestContext>();
        requestContext.SetupGet(t => t.Request).Returns(request);
        requestContext.SetupGet(t => t.ErrorHandler).Returns(
            new DefaultErrorHandler(
                Array.Empty<IErrorFilter>(),
                new RequestExecutorOptions()));
        requestContext.SetupProperty(t => t.Document);
        requestContext.SetupProperty(t => t.DocumentId);
        requestContext.SetupProperty(t => t.Exception);
        requestContext.SetupProperty(t => t.Result);

        // act
        await middleware.InvokeAsync(requestContext.Object);

        // assert
        Assert.Null(requestContext.Object.Document);
        Assert.NotNull(requestContext.Object.DocumentId);
        Assert.NotNull(requestContext.Object.Exception);
        Assert.NotNull(requestContext.Object.Result);
    }
}
