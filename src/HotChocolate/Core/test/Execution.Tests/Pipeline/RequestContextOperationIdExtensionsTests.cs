using HotChocolate.Language;

namespace HotChocolate.Execution;

public sealed class RequestContextOperationIdExtensionsTests
{
    [Fact]
    public void GetOperationId_Should_Throw_When_DocumentIsMissing()
    {
        // arrange
        var context = new PooledRequestContext();

        // act
        void Act() => context.GetOperationId();

        // assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Equal(
            "The request context has no operation document to compute an operation id from.",
            exception.Message);
    }

    [Fact]
    public void GetOperationId_Should_Throw_When_DocumentIdIsEmpty()
    {
        // arrange
        var context = new PooledRequestContext();
        context.OperationDocumentInfo.Document = Utf8GraphQLParser.Parse("{ foo }");

        // act
        void Act() => context.GetOperationId();

        // assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Equal(
            "The operation document must have a document ID before an operation id can be computed.",
            exception.Message);
    }

    [Fact]
    public void GetOperationId_Should_ReturnDocumentIdDirectly_When_SingleOperation()
    {
        // arrange
        var documentId = new OperationDocumentId("abc123");
        var context = CreateContext("{ foo }", documentId, operationName: null);

        // act
        var operationId = context.GetOperationId();

        // assert
        Assert.Same(documentId.Value, operationId);
    }

    [Fact]
    public void GetOperationId_Should_BuildIdDotOperationName_When_MultipleOperations()
    {
        // arrange
        var documentId = new OperationDocumentId("abc123");
        const string document = "query A { foo } query B { bar }";
        var context = CreateContext(document, documentId, operationName: "B");

        // act
        var operationId = context.GetOperationId();

        // assert
        Assert.Equal("abc123.B", operationId);
    }

    [Fact]
    public void GetOperationId_Should_BuildIdDotDefault_When_MultipleOperations_And_OperationNameIsNull()
    {
        // arrange
        var documentId = new OperationDocumentId("abc123");
        const string document = "query A { foo } query B { bar }";
        var context = CreateContext(document, documentId, operationName: null);

        // act
        var operationId = context.GetOperationId();

        // assert
        Assert.Equal("abc123.Default", operationId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("NamedOperation")]
    public void GetOperationId_Should_MatchFormula_When_IdExceedsStackallocThreshold(string? operationName)
    {
        // arrange
        // 400 characters comfortably exceeds the 256 char stackalloc threshold used by the
        // buffer, so this exercises the ArrayPool rental path.
        var idValue = new string('a', 400);
        var documentId = new OperationDocumentId(idValue);
        const string document = "query A { foo } query B { bar }";
        var context = CreateContext(document, documentId, operationName);

        // act
        var operationId = context.GetOperationId();

        // assert
        var expected = $"{idValue}.{operationName ?? "Default"}";
        Assert.Equal(expected, operationId);
    }

    [Fact]
    public void OperationDocumentId_Should_Reject_Dot_Character()
    {
        // act
        void Act() => new OperationDocumentId("abc.123");

        // assert
        Assert.Throws<ArgumentException>(Act);
    }

    [Fact]
    public void GetOperationId_Should_ComputeOnce_And_Cache_Result()
    {
        // arrange
        var documentId = new OperationDocumentId("abc123");
        var context = CreateContext("{ foo }", documentId, operationName: null);

        // act
        var first = context.GetOperationId();
        var second = context.GetOperationId();

        // assert
        Assert.Same(first, second);
    }

    [Fact]
    public void TryGetOperationId_Should_ReturnFalse_Before_Computed()
    {
        // arrange
        var documentId = new OperationDocumentId("abc123");
        var context = CreateContext("{ foo }", documentId, operationName: null);

        // act
        var found = context.TryGetOperationId(out var operationId);

        // assert
        Assert.False(found);
        Assert.Null(operationId);
    }

    [Fact]
    public void TryGetOperationId_Should_ReturnTrue_After_Computed()
    {
        // arrange
        var documentId = new OperationDocumentId("abc123");
        var context = CreateContext("{ foo }", documentId, operationName: null);
        var computed = context.GetOperationId();

        // act
        var found = context.TryGetOperationId(out var operationId);

        // assert
        Assert.True(found);
        Assert.Equal(computed, operationId);
    }

    private static PooledRequestContext CreateContext(
        string document,
        OperationDocumentId documentId,
        string? operationName)
    {
        var context = new PooledRequestContext();
        var request = OperationRequestBuilder.New()
            .SetDocument(document)
            .SetDocumentId(documentId)
            .SetOperationName(operationName)
            .Build();

        context.Initialize(
            schema: null!,
            executorVersion: 0,
            request: request,
            requestIndex: 0,
            requestServices: null!,
            requestAborted: CancellationToken.None);

        context.OperationDocumentInfo.Document = Utf8GraphQLParser.Parse(document);

        return context;
    }
}
