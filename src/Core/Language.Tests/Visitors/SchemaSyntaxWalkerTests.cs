using System.Collections.Generic;
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
            DocumentNode document = Parser.Default.Parse(query);

            // act
            DummySchemaSyntaxWalker walker = new DummySchemaSyntaxWalker();
            walker.Visit(document, null);

            // assert
            Assert.True(walker.VisitedAllNodes);
        }
    }
}
