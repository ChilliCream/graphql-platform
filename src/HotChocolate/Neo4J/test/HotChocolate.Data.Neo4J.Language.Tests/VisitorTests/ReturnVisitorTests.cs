using Xunit;

namespace HotChocolate.Data.Neo4J.Language
{
    public class ReturnVisitorTests
    {
        [Fact]
        public void ReturnNamedNode()
        {
            var visitor = new CypherVisitor();
            Node movie = Node.Create("Movie").Named("m");

            //Return statement = new(false, movie);
            //statement.Visit(visitor);
            //visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void ReturnDistinctNamedNode()
        {
            var visitor = new CypherVisitor();
            Node movie = Node.Create("Movie").Named("m");

            //Return statement = new(true, movie);
            //statement.Visit(visitor);
            //visitor.Print().MatchSnapshot();
        }
    }
}
