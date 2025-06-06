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
    public void Extract_Operation_Documents()
    {
        // arrange
        var query = Parse(Open("simple.query1.graphql"));
        List<DocumentNode> queries = [query];

        // act
        var operations = CreateOperationDocuments(queries);

        // assert
        Assert.Collection(
            operations.Operations,
            t => Assert.Equal("GetBookTitles", t.Key),
            t => Assert.Equal("GetBooksAndAuthor", t.Key));

        operations.Operations.Select(t => t.Value.ToString()).ToArray().MatchSnapshot();
    }

    [Fact]
    public void Merge_Multiple_Documents()
    {
        // arrange
        var query1 = Parse(Open("simple.query1.graphql"));
        var query2 = Parse(Open("simple.query2.graphql"));
        List<DocumentNode> queries = [query1, query2];

        // act
        var operations = CreateOperationDocuments(queries);

        // assert
        Assert.Collection(
            operations.Operations,
            t => Assert.Equal("GetBookTitles", t.Key),
            t => Assert.Equal("GetBooksAndAuthor", t.Key),
            t => Assert.Equal("GetAuthorsAndBooks", t.Key));

        operations.Operations.Select(t => t.Value.ToString()).ToArray().MatchSnapshot();
    }

    [Fact]
    public void No_Operation()
    {
        // arrange
        DocumentNode query = new(new List<IDefinitionNode>());
        List<DocumentNode> queries = [query];

        // act
        void Error() => CreateOperationDocuments(queries);

        // assert
        var error = Assert.Throws<ArgumentException>(Error);
        error.Message.MatchSnapshot();
    }

    [Fact]
    public void Duplicate_Operation()
    {
        // arrange
        var query1 = Parse(Open("simple.query2.graphql"));
        var query2 = Parse(Open("simple.query2.graphql"));
        List<DocumentNode> queries = [query1, query2];

        // act
        void Error() => CreateOperationDocuments(queries);

        // assert
        var error = Assert.Throws<CodeGeneratorException>(Error);
        error.Message.MatchSnapshot();
    }

    [Fact]
    public void Duplicate_Fragment()
    {
        // arrange
        var query1 = Parse(Open("simple.query1.graphql"));
        var query2 = query1.WithDefinitions(query1.Definitions.Skip(2).ToArray());
        List<DocumentNode> queries = [query1, query2];

        // act
        void Error() => CreateOperationDocuments(queries);

        // assert
        var error = Assert.Throws<CodeGeneratorException>(Error);
        error.Message.MatchSnapshot();
    }
}
