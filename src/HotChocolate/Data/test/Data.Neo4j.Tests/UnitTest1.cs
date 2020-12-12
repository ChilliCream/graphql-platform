using System;
using Xunit;
using Snapshooter.Xunit;
using HotChocolate.Data.Neo4J.Language;

namespace HotChocolate.Data.Neo4J.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            string[] labels = { "test", "test" };

            Node cypher = Cypher.Node("test", labels);
            cypher.MatchSnapshot();
        }
    }
}
