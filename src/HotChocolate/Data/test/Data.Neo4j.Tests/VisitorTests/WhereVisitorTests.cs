using HotChocolate.Data.Neo4J.Language;
using Xunit;

namespace HotChocolate.Data.Neo4J.Tests
{
    public class WhereVisitorTests
    {
        [Fact]
        public void Where()
        {
            var visitor = new CypherVisitor();

            //Where statement = Where("Movie").Named("m");
            //statement.Visit(visitor);

            //visitor.Print().MatchSnapshot();
        }
    }
}
