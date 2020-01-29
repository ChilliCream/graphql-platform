using ChilliCream.Testing;
using Xunit;

namespace HotChocolate.Language
{
    public class SchemaSyntaxWalkerTests
    {
        [Fact]
        public void Visit_KitchenSinkSchema_AllVisitMethodsAreHit()
        {
            // arrange
            string query = FileResource.Open("schema-kitchen-sink.graphql");
            DocumentNode document = Utf8GraphQLParser.Parse(query);

            // act
            var walker = new DummySchemaSyntaxWalker();
            walker.Visit(document, null);

            // assert
            Assert.True(walker.VisitedAllNodes);
        }
    }
}
