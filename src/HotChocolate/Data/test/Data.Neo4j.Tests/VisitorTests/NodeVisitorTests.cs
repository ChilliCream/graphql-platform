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

            Node movie = Cypher.Node("Movie").Named("m");
            movie.Visit(visitor);

            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void NodeWithAdditionalLabels()
        {
            string[] additionalLabels = { "Film", "Flick" };

            var visitor = new CypherVisitor();

            Node movie = Cypher.Node("Movie", additionalLabels).Named("m");
            movie.Visit(visitor);

            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void NodeWithOneProperty()
        {
            var visitor = new CypherVisitor();

            Node movie = Cypher.Node("Movie")
                                    .Named("m")
                                    .WithProperties(
                                        new Dictionary<string, ILiteral>()
                                        {
                                            {"Title", Cypher.StringLiteral("The Matrix")}
                                        });
            movie.Visit(visitor);

            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void NodeWithMultipleProperties()
        {
            var visitor = new CypherVisitor();

            Node movie = Cypher.Node("Movie")
                                    .Named("m")
                                    .WithProperties(
                                        new Dictionary<string, ILiteral>()
                                        {
                                            {"Released", Cypher.LiteralTrue()},
                                            {"Title", Cypher.StringLiteral("The Matrix")},
                                            {"Director", Cypher.Null()}
                                        });
            movie.Visit(visitor);

            visitor.Print().MatchSnapshot();
        }
    }
}
