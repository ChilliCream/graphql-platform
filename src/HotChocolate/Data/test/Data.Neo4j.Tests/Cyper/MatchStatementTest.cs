using System;
using HotChocolate.Data.Neo4j;
using Snapshooter.Xunit;
using Xunit;

namespace Data.Neo4j.Tests
{
    public class MatchStatementTest
    {
        [Fact]
        public void Test1()
        {
            Node node = Cypher.Node("p", "Movie");

            var cypher = new Cypher();
            cypher.Match(node);

            cypher.Print().MatchSnapshot();
        }
    }
}
