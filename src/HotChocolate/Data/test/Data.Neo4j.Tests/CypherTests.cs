using HotChocolate.Data.Neo4J.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Neo4J.Tests
{
    public class CypherTests
    {
        [Fact]
        public void MatchNamedNode()
        {
            Node movie = Cypher.Node("Movie").Named("m");
            StatementBuilder? statement = Cypher.Match(movie);
                                                //.Return(movie);
            statement.Build().MatchSnapshot();
        }

        [Fact]
        public void MatchNamedNodeWithProperties()
        {
            Node movie = Cypher.Node("Movie")
                .Named("m")
                .WithProperties(
                    "title", Cypher.LiteralOf("The Matrix"),
                    "yearReleased", Cypher.LiteralOf(1999),
                    "released", Cypher.LiteralOf(true),
                    "rating", Cypher.LiteralOf(8.7)
                );

            StatementBuilder? statement = Cypher.Match(movie);
            statement.Build().MatchSnapshot();
        }

        [Fact]
        public void MatchTwoNamedNode()
        {
            Node movie = Node.Create("Movie").Named("m");
            Node bike = Node.Create("Bike").Named("b");

            StatementBuilder? statement = Cypher.Match(movie, bike);
            statement.Build().MatchSnapshot();
        }
    }
}
