using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Neo4j.Tests
{
    public class MatchClauseTest
    {
        [Fact]
        public void Match_Optional_Default()
        {
            Node node = new Node("Speaker").Named("m");
            var match = new Match(node);

            match.MatchSnapshot();
        }

        [Fact]
        public void Match_Optional_True()
        {
            Node node = new Node("Speaker").Named("m");
            var match = new Match(node, true);

            match.MatchSnapshot();
        }
    }
}
