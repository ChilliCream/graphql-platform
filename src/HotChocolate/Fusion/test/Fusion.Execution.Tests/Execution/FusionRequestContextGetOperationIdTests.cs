using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

public sealed class FusionRequestContextGetOperationIdTests
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
        Assert.Equal("The operation document is not available in the context.", exception.Message);
    }

    [Fact]
    public void GetOperationId_Should_Throw_When_DocumentHashIsEmpty()
    {
        // arrange
        var context = new PooledRequestContext();
        context.OperationDocumentInfo.Document = Utf8GraphQLParser.Parse("{ foo }");

        // act
        void Act() => context.GetOperationId();

        // assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Equal("The operation document hash is not available in the context.", exception.Message);
    }

    [Fact]
    public void GetOperationId_Should_ReturnHashValueDirectly_When_SingleOperation()
    {
        // arrange
        var hash = new OperationDocumentHash("abc123", "md5", HashFormat.Hex);
        var context = CreateContext("{ foo }", hash, operationName: null);

        // act
        var operationId = context.GetOperationId();

        // assert
        Assert.Same(hash.Value, operationId);
    }

    [Fact]
    public void GetOperationId_Should_BuildHashDotOperationName_When_MultipleOperations()
    {
        // arrange
        var hash = new OperationDocumentHash("abc123", "md5", HashFormat.Hex);
        const string document = "query A { foo } query B { bar }";
        var context = CreateContext(document, hash, operationName: "B");

        // act
        var operationId = context.GetOperationId();

        // assert
        Assert.Equal("abc123.B", operationId);
        Assert.Equal($"{hash.Value}.{"B"}", operationId);
    }

    [Fact]
    public void GetOperationId_Should_BuildHashDotDefault_When_MultipleOperations_And_OperationNameIsNull()
    {
        // arrange
        var hash = new OperationDocumentHash("abc123", "md5", HashFormat.Hex);
        const string document = "query A { foo } query B { bar }";
        var context = CreateContext(document, hash, operationName: null);

        // act
        var operationId = context.GetOperationId();

        // assert
        Assert.Equal("abc123.Default", operationId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("NamedOperation")]
    public void GetOperationId_Should_MatchPreviousInterpolationFormula_When_HashExceedsStackallocThreshold(
        string? operationName)
    {
        // arrange
        // 400 characters comfortably exceeds the 256 char stackalloc threshold used by the
        // buffer, so this exercises the ArrayPool rental path.
        var hashValue = new string('h', 400);
        var hash = new OperationDocumentHash(hashValue, "sha256", HashFormat.Hex);
        const string document = "query A { foo } query B { bar }";
        var context = CreateContext(document, hash, operationName);

        // act
        var operationId = context.GetOperationId();

        // assert
        var expected = $"{hashValue}.{operationName ?? "Default"}";
        Assert.Equal(expected, operationId);
    }

    private static PooledRequestContext CreateContext(
        string document,
        OperationDocumentHash hash,
        string? operationName)
    {
        var context = new PooledRequestContext();
        var request = OperationRequestBuilder.New()
            .SetDocument(document)
            .SetDocumentHash(hash)
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
