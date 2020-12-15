using HotChocolate.Data.Neo4J.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Neo4J.Tests
{
    public class CypherTests
    {
        [Fact]
        public void MatchClause()
        {
            string[] additionalLabels = { "Tricycle", "MotorCycle" };



            // Node bike = Cypher.Node("Bike", additionalLabels).Named("b");
            // Cypher
            // bike.Visit(visitor);
            //
            // visitor.Print().MatchSnapshot();
        }

    }
}
