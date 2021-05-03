using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using HotChocolate.Validation;
using Moq;
using Xunit;

namespace HotChocolate.Execution.Pipeline
{
    public class DocumentValidationMiddlewareTests
    {
        [Fact]
        public async Task DocumentIsValidated_SkipValidation()
        {
            // arrange
            var validator = new Mock<IDocumentValidator>();
            validator.Setup(t => t.Validate(
                It.IsAny<ISchema>(),
                It.IsAny<DocumentNode>(),
                It.IsAny<IEnumerable<KeyValuePair<string, object>>>()))
                .Returns(DocumentValidatorResult.Ok);

            var middleware = new DocumentValidationMiddleware(
                context => default,
                new NoopDiagnosticEvents(),
                validator.Object);

            var request = QueryRequestBuilder.New()
                .SetQuery("{ a }")
                .SetQueryId("a")
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse("{ a }");
            var validationResult = new DocumentValidatorResult(Array.Empty<IError>());

            var requestContext = new Mock<IRequestContext>();
            requestContext.SetupGet(t => t.Request).Returns(request);
            requestContext.SetupGet(t => t.Schema).Returns(default(ISchema));
            requestContext.SetupProperty(t => t.Document, document);
            requestContext.SetupProperty(t => t.ValidationResult, validationResult);

            // act
            await middleware.InvokeAsync(requestContext.Object);

            // assert
            Assert.Equal(validationResult, requestContext.Object.ValidationResult);
        }

        [Fact]
        public async Task DocumentNeedsValidation_DocumentIsValid()
        {
            // arrange
            var validator = new Mock<IDocumentValidator>();
            validator.Setup(t => t.Validate(
                It.IsAny<ISchema>(),
                It.IsAny<DocumentNode>(),
                It.IsAny<IEnumerable<KeyValuePair<string, object>>>()))
                .Returns(DocumentValidatorResult.Ok);

            var middleware = new DocumentValidationMiddleware(
                context => default,
                new NoopDiagnosticEvents(),
                validator.Object);

            var request = QueryRequestBuilder.New()
                .SetQuery("{ a }")
                .SetQueryId("a")
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse("{ a }");

            var requestContext = new Mock<IRequestContext>();
            requestContext.SetupGet(t => t.Request).Returns(request);
            requestContext.SetupGet(t => t.Schema).Returns(default(ISchema));
            requestContext.SetupGet(t => t.ContextData).Returns(new Dictionary<string, object>());
            requestContext.SetupProperty(t => t.Document, document);
            requestContext.SetupProperty(t => t.ValidationResult);

            // act
            await middleware.InvokeAsync(requestContext.Object);

            // assert
            Assert.Equal(DocumentValidatorResult.Ok, requestContext.Object.ValidationResult);
        }

        [Fact]
        public async Task DocumentNeedsValidation_DocumentInvalid()
        {
            // arrange
            var validationResult = new DocumentValidatorResult(
               new[] { ErrorBuilder.New().SetMessage("Foo").Build() });

            var validator = new Mock<IDocumentValidator>();
            validator.Setup(t => t.Validate(
                    It.IsAny<ISchema>(),
                    It.IsAny<DocumentNode>(),
                    It.IsAny<IEnumerable<KeyValuePair<string, object>>>()))
                .Returns(validationResult);

            var middleware = new DocumentValidationMiddleware(
                context => throw new Exception("Should not be called."),
                new NoopDiagnosticEvents(),
                validator.Object);

            var request = QueryRequestBuilder.New()
                .SetQuery("{ a }")
                .SetQueryId("a")
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse("{ a }");

            var requestContext = new Mock<IRequestContext>();
            requestContext.SetupGet(t => t.Request).Returns(request);
            requestContext.SetupGet(t => t.Schema).Returns(default(ISchema));
            requestContext.SetupGet(t => t.ContextData).Returns(new Dictionary<string, object>());
            requestContext.SetupProperty(t => t.Document, document);
            requestContext.SetupProperty(t => t.ValidationResult);
            requestContext.SetupProperty(t => t.Result);

            // act
            await middleware.InvokeAsync(requestContext.Object);

            // assert
            Assert.NotNull(requestContext.Object.ValidationResult);
            Assert.NotNull(requestContext.Object.Result);
        }

        [Fact]
        public async Task NoDocument_MiddlewareWillFail()
        {
            // arrange
            var validator = new Mock<IDocumentValidator>();
            validator.Setup(t => t.Validate(
                It.IsAny<ISchema>(),
                It.IsAny<DocumentNode>(),
                It.IsAny<IEnumerable<KeyValuePair<string, object>>>()))
                .Returns(DocumentValidatorResult.Ok);

            var middleware = new DocumentValidationMiddleware(
                context => throw new Exception("Should not be called."),
                new NoopDiagnosticEvents(),
                validator.Object);

            var request = QueryRequestBuilder.New()
                .SetQuery("{ a }")
                .SetQueryId("a")
                .Create();

            var requestContext = new Mock<IRequestContext>();
            requestContext.SetupGet(t => t.Request).Returns(request);
            requestContext.SetupGet(t => t.Schema).Returns(default(ISchema));
            requestContext.SetupProperty(t => t.Document);
            requestContext.SetupProperty(t => t.Result);

            // act
            await middleware.InvokeAsync(requestContext.Object);

            // assert
            Assert.NotNull(requestContext.Object.Result);
        }
    }
}
