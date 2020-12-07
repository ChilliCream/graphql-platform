using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;
using static ChilliCream.Testing.FileResource;
using static HotChocolate.Language.Utf8GraphQLParser;
using static StrawberryShake.CodeGeneration.Utilities.OperationDocumentHelper;

namespace StrawberryShake.CodeGeneration.Utilities
{
    public class OperationDocumentHelperTests
    {
        // This test ensures that each operation becomes one document that
        // only has the fragments needed by the extracted operation.
        [Fact]
        public void Extract_Operation_Documents()
        {
            // arrange
            DocumentNode query = Parse(Open("simple.query1.graphql"));
            List<DocumentNode> queries = new() { query };

            // act
            OperationDocuments operations = CreateOperationDocuments(queries);

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
            DocumentNode query1 = Parse(Open("simple.query1.graphql"));
            DocumentNode query2 = Parse(Open("simple.query2.graphql"));
            List<DocumentNode> queries = new() { query1, query2 };

            // act
            OperationDocuments operations = CreateOperationDocuments(queries);

            // assert
            Assert.Collection(
                operations.Operations,
                t => Assert.Equal("GetBookTitles", t.Key),
                t => Assert.Equal("GetBooksAndAuthor", t.Key),
                t => Assert.Equal("GetAuthorsAndBooks", t.Key));

            operations.Operations.Select(t => t.Value.ToString()).ToArray().MatchSnapshot();
        }
    }
}
