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

    [Theory]
    [InlineData("query Foo { foo }", "Bar")]
    [InlineData("{ foo }", "Bar")]
    public void GetOperationId_Should_Throw_When_SingleOperation_And_NameIsNotDefined(
        string document,
        string operationName)
    {
        // arrange
        var context = CreateContext(document, new OperationDocumentId("abc123"), operationName);

        // act
        void Act() => context.GetOperationId();

        // assert
        var exception = Assert.Throws<GraphQLException>(Act);
        Assert.Equal(
            "The specified operation `Bar` cannot be found.",
            Assert.Single(exception.Errors).Message);
    }

    [Fact]
    public void GetOperationId_Should_ReturnDocumentId_When_SingleOperation_And_NameIsDefined()
    {
        // arrange
        var documentId = new OperationDocumentId("abc123");
        var context = CreateContext("query Foo { foo }", documentId, operationName: "Foo");

        // act
        var operationId = context.GetOperationId();

        // assert
        Assert.Same(documentId.Value, operationId);
    }

    [Fact]
    public void GetOperationId_Should_Throw_When_DocumentHasNoOperation_And_NameIsOmitted()
    {
        // arrange
        var context = CreateContext(
            "fragment F on Query { foo }",
            new OperationDocumentId("abc123"),
            operationName: null);

        // act
        void Act() => context.GetOperationId();

        // assert
        var exception = Assert.Throws<GraphQLException>(Act);
        var error = Assert.Single(exception.Errors);
        Assert.Equal("There are no operations in the GraphQL document.", error.Message);
        Assert.Equal(1, Assert.Single(error.Locations!).Line);
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetOperationId_Should_Throw_When_MultipleOperations_And_OperationNameIsOmitted(
        string? operationName)
    {
        // arrange
        const string document = "query Default { foo } query B { bar }";
        var context = CreateContext(document, new OperationDocumentId("abc123"), operationName);

        // act
        void Act() => context.GetOperationId();

        // assert
        var exception = Assert.Throws<GraphQLException>(Act);
        Assert.Equal(
            "The operation name can only be omitted if there is just one operation in a GraphQL document.",
            Assert.Single(exception.Errors).Message);
    }

    [Fact]
    public void GetOperationId_Should_MatchFormula_When_IdExceedsStackallocThreshold()
    {
        // arrange
        // 400 characters comfortably exceeds the 256 char stackalloc threshold used by the
        // buffer, so this exercises the ArrayPool rental path.
        var idValue = new string('a', 400);
        var documentId = new OperationDocumentId(idValue);
        const string document = "query A { foo } query B { bar }";
        var context = CreateContext(document, documentId, operationName: "B");

        // act
        var operationId = context.GetOperationId();

        // assert
        var expected = $"{idValue}.B";
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

    [Fact]
    public void GetOperationId_Should_BeCleared_After_Reset()
    {
        // arrange
        var documentId = new OperationDocumentId("abc123");
        var context = CreateContext("{ foo }", documentId, operationName: null);
        context.GetOperationId();

        // act
        context.Reset();

        // assert
        Assert.False(context.TryGetOperationId(out _));
    }

    [Fact]
    public void OperationCount_Should_Reflect_Replaced_Document()
    {
        // arrange
        var context = new PooledRequestContext();
        context.OperationDocumentInfo.Document = Utf8GraphQLParser.Parse("{ foo }");
        var initialCount = context.OperationDocumentInfo.OperationCount;

        // act
        context.OperationDocumentInfo.Document = Utf8GraphQLParser.Parse("query A { foo } query B { bar }");

        // assert
        Assert.Equal(1, initialCount);
        Assert.Equal(2, context.OperationDocumentInfo.OperationCount);
    }

    [Fact]
    public void OperationId_Should_Clear_When_Document_Is_Replaced()
    {
        // arrange
        var documentId = new OperationDocumentId("abc123");
        var context = CreateContext("{ foo }", documentId, operationName: null);
        context.GetOperationId();

        // act
        context.OperationDocumentInfo.Document = Utf8GraphQLParser.Parse("{ bar }");

        // assert
        Assert.False(context.TryGetOperationId(out _));
    }

    [Fact]
    public void OperationId_Should_Clear_When_Id_Is_Replaced()
    {
        // arrange
        var documentId = new OperationDocumentId("abc123");
        var context = CreateContext("{ foo }", documentId, operationName: null);
        context.GetOperationId();

        // act
        context.OperationDocumentInfo.Id = new OperationDocumentId("xyz789");

        // assert
        Assert.False(context.TryGetOperationId(out _));
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
