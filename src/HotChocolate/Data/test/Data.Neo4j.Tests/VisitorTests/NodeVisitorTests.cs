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

            Node bike = Cypher.Node("Bike").Named("b");
            bike.Visit(visitor);

            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void NodeWithAdditionalLabels()
        {
            string[] additionalLabels = { "Tricycle", "MotorCycle" };

            var visitor = new CypherVisitor();

            Node bike = Cypher.Node("Bike", additionalLabels).Named("b");
            bike.Visit(visitor);

            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void NodeWithOneProperty()
        {
            var visitor = new CypherVisitor();

            Node bike = Cypher.Node("Bike")
                                    .Named("b")
                                    .WithProperties(new object[] {"Serviced", Cypher.LiteralTrue()});
            bike.Visit(visitor);

            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void NodeWithMultipleProperties()
        {
            var visitor = new CypherVisitor();

            Node bike = Cypher.Node("Bike")
                                    .Named("b")
                                    .WithProperties(new object[]
                                    {
                                        "Serviced", Cypher.LiteralTrue(),
                                        "ServiceNote", Cypher.StringLiteral("Help is needed!")
                                    });
            bike.Visit(visitor);

            visitor.Print().MatchSnapshot();
        }
    }
}
