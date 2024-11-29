using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using HotChocolate.Validation;
using Moq;

namespace HotChocolate.Execution.Pipeline;

public class DocumentValidationMiddlewareTests
{
    [Fact]
    public async Task DocumentIsValidated_SkipValidation()
    {
        // arrange
        var validator = new Mock<IDocumentValidator>();
        validator.SetupGet(t => t.HasDynamicRules).Returns(false);
        validator.Setup(t => t.ValidateAsync(
                It.IsAny<ISchema>(),
                It.IsAny<DocumentNode>(),
                It.IsAny<OperationDocumentId>(),
                It.IsAny<IDictionary<string, object?>>(),
                It.Is<bool>(b => true),
                It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<DocumentValidatorResult>(DocumentValidatorResult.Ok));

        var middleware = DocumentValidationMiddleware.Create(
            _ => default,
            new NoopExecutionDiagnosticEvents(),
            validator.Object);

        var request = OperationRequestBuilder.New()
            .SetDocument("{ a }")
            .SetDocumentId("a")
            .Build();

        var document = Utf8GraphQLParser.Parse("{ a }");
        var validationResult = new DocumentValidatorResult(Array.Empty<IError>());

        var requestContext = new Mock<IRequestContext>();
        requestContext.SetupGet(t => t.Request).Returns(request);
        requestContext.SetupProperty(t => t.Document, document);
        requestContext.SetupProperty(t => t.ValidationResult, validationResult);

        // act
        await middleware.InvokeAsync(requestContext.Object);

        // assert
        Assert.Equal(validationResult, requestContext.Object.ValidationResult);
        Assert.False(requestContext.Object.ValidationResult!.HasErrors);
    }

    [Fact]
    public async Task DocumentIsValidated_Dynamic()
    {
        // arrange
        var validator = new Mock<IDocumentValidator>();
        validator.SetupGet(t => t.HasDynamicRules).Returns(true);
        validator.Setup(t => t.ValidateAsync(
                It.IsAny<ISchema>(),
                It.IsAny<DocumentNode>(),
                It.IsAny<OperationDocumentId>(),
                It.IsAny<IDictionary<string, object?>>(),
                It.Is<bool>(b => true),
                It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<DocumentValidatorResult>(DocumentValidatorResult.Ok));

        var middleware = DocumentValidationMiddleware.Create(
            _ => default,
            new NoopExecutionDiagnosticEvents(),
            validator.Object);

        var request = OperationRequestBuilder.New()
            .SetDocument("{ a }")
            .SetDocumentId("a")
            .Build();

        var document = Utf8GraphQLParser.Parse("{ a }");
        var validationResult = new DocumentValidatorResult(Array.Empty<IError>());

        var requestContext = new Mock<IRequestContext>();
        requestContext.SetupGet(t => t.Request).Returns(request);
        requestContext.SetupGet(t => t.ContextData).Returns(new Dictionary<string, object?>());
        requestContext.SetupProperty(t => t.Document, document);
        requestContext.SetupProperty(t => t.DocumentId, "abc");
        requestContext.SetupProperty(t => t.ValidationResult, validationResult);

        // act
        await middleware.InvokeAsync(requestContext.Object);

        // assert
        Assert.NotEqual(validationResult, requestContext.Object.ValidationResult);
        Assert.False(requestContext.Object.ValidationResult!.HasErrors);
    }

    [Fact]
    public async Task DocumentNeedsValidation_DocumentIsValid()
    {
        // arrange
        var validator = new Mock<IDocumentValidator>();
        validator.Setup(t => t.ValidateAsync(
                It.IsAny<ISchema>(),
                It.IsAny<DocumentNode>(),
                It.IsAny<OperationDocumentId>(),
                It.IsAny<IDictionary<string, object?>>(),
                It.Is<bool>(b => true),
                It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<DocumentValidatorResult>(DocumentValidatorResult.Ok));

        var middleware = DocumentValidationMiddleware.Create(
            _ => default,
            new NoopExecutionDiagnosticEvents(),
            validator.Object);

        var request = OperationRequestBuilder.New()
            .SetDocument("{ a }")
            .SetDocumentId("a")
            .Build();

        var document = Utf8GraphQLParser.Parse("{ a }");

        var requestContext = new Mock<IRequestContext>();
        requestContext.SetupGet(t => t.Request).Returns(request);
        requestContext.SetupGet(t => t.ContextData).Returns(new Dictionary<string, object?>());
        requestContext.SetupProperty(t => t.Document, document);
        requestContext.SetupProperty(t => t.DocumentId, "abc");
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
            new[] { ErrorBuilder.New().SetMessage("Foo").Build(), });

        var validator = new Mock<IDocumentValidator>();
        validator.Setup(t => t.ValidateAsync(
                It.IsAny<ISchema>(),
                It.IsAny<DocumentNode>(),
                It.IsAny<OperationDocumentId>(),
                It.IsAny<IDictionary<string, object?>>(),
                It.Is<bool>(b => true),
                It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<DocumentValidatorResult>(validationResult));

        var middleware = DocumentValidationMiddleware.Create(
            _ => throw new Exception("Should not be called."),
            new NoopExecutionDiagnosticEvents(),
            validator.Object);

        var request = OperationRequestBuilder.New()
            .SetDocument("{ a }")
            .SetDocumentId("a")
            .Build();

        var document = Utf8GraphQLParser.Parse("{ a }");

        var requestContext = new Mock<IRequestContext>();
        requestContext.SetupGet(t => t.Request).Returns(request);
        requestContext.SetupGet(t => t.ContextData).Returns(new Dictionary<string, object?>());
        requestContext.SetupProperty(t => t.Document, document);
        requestContext.SetupProperty(t => t.DocumentId, "abc");
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
        validator.Setup(t => t.ValidateAsync(
                It.IsAny<ISchema>(),
                It.IsAny<DocumentNode>(),
                It.IsAny<OperationDocumentId>(),
                It.IsAny<IDictionary<string, object?>>(),
                It.Is<bool>(b => true),
                It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<DocumentValidatorResult>(DocumentValidatorResult.Ok));

        var middleware = DocumentValidationMiddleware.Create(
            _ => throw new Exception("Should not be called."),
            new NoopExecutionDiagnosticEvents(),
            validator.Object);

        var request = OperationRequestBuilder.New()
            .SetDocument("{ a }")
            .SetDocumentId("a")
            .Build();

        var requestContext = new Mock<IRequestContext>();
        requestContext.SetupGet(t => t.Request).Returns(request);
        requestContext.SetupProperty(t => t.Document);
        requestContext.SetupProperty(t => t.Result);

        // act
        await middleware.InvokeAsync(requestContext.Object);

        // assert
        Assert.NotNull(requestContext.Object.Result);
    }
}
