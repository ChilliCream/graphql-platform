using ChilliCream.Testing;
using Xunit;

namespace HotChocolate.Language
{
    public class QuerySyntaxWalkerTests
    {
        [Fact]
        public void Visit_KitchenSinkQuery_AllVisitMethodsAreHit()
        {
            // arrange
            string query = FileResource.Open("kitchen-sink.graphql");
            DocumentNode document = Utf8GraphQLParser.Parse(query);

            // act
            var walker = new DummyQuerySyntaxWalker();
            walker.Visit(document, null);

            // assert
            Assert.True(walker.VisitedAllNodes);
        }
    }
}
