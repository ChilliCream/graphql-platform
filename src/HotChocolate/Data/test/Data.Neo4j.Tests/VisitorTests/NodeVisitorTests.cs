using System.Collections.Generic;
using HotChocolate.Data.Neo4J.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Neo4J.Tests
{
    public class NodeVisitorTests
    {
        [Fact]
        public void NamedNode()
        {
            var visitor = new CypherVisitor();

            Node movie = Node.Create("Movie").Named("m");
            movie.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void NodeWithAdditionalLabels()
        {
            string[] additionalLabels = { "Film", "Flick" };

            var visitor = new CypherVisitor();

            Node movie = Node.Create("Movie", additionalLabels).Named("m");
            movie.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void NodeWithOneProperty()
        {
            var visitor = new CypherVisitor();

            Node movie = Node.Create("Movie")
                                    .Named("m")
                                    .WithProperties("Title", Cypher.LiteralOf("The Matrix"));
            movie.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void NodeWithMultipleProperties()
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
            movie.Visit(visitor);

            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void NodeProperty()
        {
            var visitor = new CypherVisitor();

            Node movie = Node.Create("Movie").Named("m");

            Comparison comparison = new(movie.Property("Title"), Operator.Equality, (Cypher.LiteralOf("The Matrix")));
            comparison.Visit(visitor);

            visitor.Print().MatchSnapshot();
        }

    }
}
