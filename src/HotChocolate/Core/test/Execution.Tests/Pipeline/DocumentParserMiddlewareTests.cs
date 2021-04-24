using System;
using System.Threading.Tasks;
using HotChocolate.Execution.Errors;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Options;
using HotChocolate.Language;
using Moq;
using Xunit;

namespace HotChocolate.Execution.Pipeline
{
    public class DocumentParserMiddlewareTests
    {
        [Fact]
        public async Task DocumentExists_SkipParsing_DocumentIsUnchanged()
        {
            // arrange
            var cache = new Caching.DefaultDocumentCache();
            var hashProvider = new MD5DocumentHashProvider();

            var middleware = new DocumentParserMiddleware(
                context => default,
                new NoopDiagnosticEvents(),
                cache,
                hashProvider);

            var request = QueryRequestBuilder.New()
                .SetQuery("{ a }")
                .SetQueryId("a")
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse("{ a }");

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
            var cache = new Caching.DefaultDocumentCache();
            var hashProvider = new MD5DocumentHashProvider();

            var middleware = new DocumentParserMiddleware(
                context => default,
                new NoopDiagnosticEvents(),
                cache,
                hashProvider);

            var request = QueryRequestBuilder.New()
                .SetQuery("{ a }")
                .SetQueryId("a")
                .Create();

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
            var cache = new Caching.DefaultDocumentCache();
            var hashProvider = new MD5DocumentHashProvider();

            var middleware = new DocumentParserMiddleware(
                context => throw new Exception("Should not be invoked."),
                new NoopDiagnosticEvents(),
                cache,
                hashProvider);

            var request = QueryRequestBuilder.New()
                .SetQuery("{")
                .SetQueryId("a")
                .Create();

            var requestContext = new Mock<IRequestContext>();
            requestContext.SetupGet(t => t.Request).Returns(request);
            requestContext.SetupGet(t => t.ErrorHandler).Returns(
                new DefaultErrorHandler(
                    Array.Empty<IErrorFilter>(),
                    new RequestExecutorAnalyzerOptions()));
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
}
