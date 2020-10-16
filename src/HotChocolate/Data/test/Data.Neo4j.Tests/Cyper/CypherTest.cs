using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Neo4j.Tests
{
    public class CypherTest
    {
        [Fact]
        public void Match_Optional_Default()
        {
            Node m = new Node("Speaker").Named("m");
            Cypher cypher = new Cypher().Match(m).Return(m);

            cypher.Print().MatchSnapshot();
        }
    }
}
