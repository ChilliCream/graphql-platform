using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Neo4J.Language
{
    public class MatchVisitorTests
    {
        [Fact]
        public void MatchNamedNode()
        {
            var visitor = new CypherVisitor();

            Node movie = Node.Create("Movie").Named("m");
            Match statement = new(false, new Pattern(movie), null);
            statement.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void MatchNamedNodeWithProperties()
        {
            var visitor = new CypherVisitor();

            Node movie = Node.Create("Movie")
                .Named("m")
                .WithProperties(
                    "Title", Cypher.LiteralOf("The Matrix"),
                    "YearReleased", Cypher.LiteralOf(1999),
                    "Released", Cypher.LiteralOf(true),
                    "Rating", Cypher.LiteralOf(8.7)
                );
            Match statement = new(false, new Pattern(movie), null);
            statement.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }
    }
}
