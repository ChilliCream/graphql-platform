using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using Moq;
using Xunit;

namespace HotChocolate.Execution.Pipeline
{
    public class DocumentCacheMiddlewareTests
    {
        [Fact]
        public async Task RetrieveItemFromCache_DocumentFoundOnCache()
        {
            var cache = new Caching.DefaultDocumentCache();
            var hashProvider = new MD5DocumentHashProvider();

            var middleware = new DocumentCacheMiddleware(
                context => default,
                new NoopDiagnosticEvents(),
                cache,
                hashProvider);

            var request = QueryRequestBuilder.New()
                .SetQuery("{ a }")
                .SetQueryName("a")
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse("{ a }");
            cache.TryAddDocument("a", document);

            var requestContext = new Mock<IRequestContext>();
            requestContext.SetupGet(t => t.Request).Returns(request);
            requestContext.SetupProperty(t => t.DocumentId);
            requestContext.SetupProperty(t => t.Document);
            requestContext.SetupProperty(t => t.ValidationResult);

            await middleware.InvokeAsync(requestContext.Object);

            Assert.Equal(document, requestContext.Object.Document);
            Assert.Equal("a", requestContext.Object.DocumentId);
        }

        [Fact]
        public async Task RetrieveItemFromCacheByHash_DocumentFoundOnCache()
        {
            var cache = new Caching.DefaultDocumentCache();
            var hashProvider = new MD5DocumentHashProvider();

            var middleware = new DocumentCacheMiddleware(
                context => default,
                new NoopDiagnosticEvents(),
                cache,
                hashProvider);

            var request = QueryRequestBuilder.New()
                .SetQuery("{ a }")
                .SetQueryHash("a")
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse("{ a }");
            cache.TryAddDocument("a", document);

            var requestContext = new Mock<IRequestContext>();
            requestContext.SetupGet(t => t.Request).Returns(request);
            requestContext.SetupProperty(t => t.DocumentId);
            requestContext.SetupProperty(t => t.Document);
            requestContext.SetupProperty(t => t.ValidationResult);

            await middleware.InvokeAsync(requestContext.Object);

            Assert.Equal(document, requestContext.Object.Document);
            Assert.Equal("a", requestContext.Object.DocumentId);
        }

        [Fact]
        public async Task RetrieveItemFromCache_DocumentNotFoundOnCache()
        {
            var cache = new Caching.DefaultDocumentCache();
            var hashProvider = new MD5DocumentHashProvider();

            var middleware = new DocumentCacheMiddleware(
                context => default,
                new NoopDiagnosticEvents(),
                cache,
                hashProvider);

            var request = QueryRequestBuilder.New()
                .SetQuery("{ a }")
                .SetQueryName("a")
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse("{ a }");
            cache.TryAddDocument("b", document);

            var requestContext = new Mock<IRequestContext>();
            requestContext.SetupGet(t => t.Request).Returns(request);
            requestContext.SetupProperty(t => t.DocumentId);
            requestContext.SetupProperty(t => t.Document);
            requestContext.SetupProperty(t => t.ValidationResult);

            await middleware.InvokeAsync(requestContext.Object);

            Assert.Null(requestContext.Object.Document);
            Assert.Null(requestContext.Object.DocumentId);
        }

        [Fact]
        public async Task AddItemToCache()
        {
            var cache = new Caching.DefaultDocumentCache();
            var hashProvider = new MD5DocumentHashProvider();

            var request = QueryRequestBuilder.New()
                .SetQuery("{ a }")
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse("{ a }");

            var middleware = new DocumentCacheMiddleware(
                context =>
                {
                    context.Document = document;
                    context.DocumentId = "a";
                    return default;
                },
                new NoopDiagnosticEvents(),
                cache,
                hashProvider);

            var requestContext = new Mock<IRequestContext>();
            requestContext.SetupGet(t => t.Request).Returns(request);
            requestContext.SetupProperty(t => t.DocumentId);
            requestContext.SetupProperty(t => t.Document);
            requestContext.SetupProperty(t => t.ValidationResult);

            await middleware.InvokeAsync(requestContext.Object);

            Assert.Equal(document, requestContext.Object.Document);
            Assert.Equal("a", requestContext.Object.DocumentId);
        }
    }
}