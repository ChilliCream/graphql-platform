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
}
