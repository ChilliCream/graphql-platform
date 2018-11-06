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
            DocumentNode document = Parser.Default.Parse(query);

            // act
            DummyQuerySyntaxWalker walker = new DummyQuerySyntaxWalker();
            walker.Visit(document);

            // assert
            Assert.True(walker.VisitedAllNodes);
        }
    }
}
