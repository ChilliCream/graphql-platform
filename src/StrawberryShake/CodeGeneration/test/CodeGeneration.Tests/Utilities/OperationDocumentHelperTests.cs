using HotChocolate.Language;
using Snapshooter.Xunit;
using static ChilliCream.Testing.FileResource;
using static HotChocolate.Language.Utf8GraphQLParser;
using static StrawberryShake.CodeGeneration.Utilities.OperationDocumentHelper;

namespace StrawberryShake.CodeGeneration.Utilities;

public class OperationDocumentHelperTests
{
    // This test ensures that each operation becomes one document that
    // only has the fragments needed by the extracted operation.
    [Fact]
    public async Task Extract_Operation_Documents()
    {
        // arrange
        var query = Parse(Open("simple.query1.graphql"));
        List<DocumentNode> queries = [query,];

        // act
        var operations = await CreateOperationDocumentsAsync(queries);

        // assert
        Assert.Collection(
            operations.Operations,
            t => Assert.Equal("GetBookTitles", t.Key),
            t => Assert.Equal("GetBooksAndAuthor", t.Key));

        operations.Operations.Select(t => t.Value.ToString()).ToArray().MatchSnapshot();
    }

    [Fact]
    public async Task Merge_Multiple_Documents()
    {
        // arrange
        var query1 = Parse(Open("simple.query1.graphql"));
        var query2 = Parse(Open("simple.query2.graphql"));
        List<DocumentNode> queries = [query1, query2,];

        // act
        var operations = await CreateOperationDocumentsAsync(queries);

        // assert
        Assert.Collection(
            operations.Operations,
            t => Assert.Equal("GetBookTitles", t.Key),
            t => Assert.Equal("GetBooksAndAuthor", t.Key),
            t => Assert.Equal("GetAuthorsAndBooks", t.Key));

        operations.Operations.Select(t => t.Value.ToString()).ToArray().MatchSnapshot();
    }

    [Fact]
    public async Task No_Operation()
    {
        // arrange
        DocumentNode query = new(new List<IDefinitionNode>());
        List<DocumentNode> queries = [query,];

        // act
        async Task Error() => await CreateOperationDocumentsAsync(queries);

        // assert
        var error = await Assert.ThrowsAsync<ArgumentException>(Error);
        error.Message.MatchSnapshot();
    }

    [Fact]
    public async Task Duplicate_Operation()
    {
        // arrange
        var query1 = Parse(Open("simple.query2.graphql"));
        var query2 = Parse(Open("simple.query2.graphql"));
        List<DocumentNode> queries = [query1, query2,];

        // act
        async Task Error() => await CreateOperationDocumentsAsync(queries);

        // assert
        var error = await Assert.ThrowsAsync<CodeGeneratorException>(Error);
        error.Message.MatchSnapshot();
    }

    [Fact]
    public async Task Duplicate_Fragment()
    {
        // arrange
        var query1 = Parse(Open("simple.query1.graphql"));
        var query2 = query1.WithDefinitions(query1.Definitions.Skip(2).ToArray());
        List<DocumentNode> queries = [query1, query2,];

        // act
        async Task Error() => await CreateOperationDocumentsAsync(queries);

        // assert
        var error = await Assert.ThrowsAsync<CodeGeneratorException>(Error);
        error.Message.MatchSnapshot();
    }
}
