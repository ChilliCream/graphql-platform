using HotChocolate.Data.Neo4J.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Neo4J.Tests
{
    public class MatchVisitorTests
    {
        [Fact]
        public void MatchNamedNode()
        {
            var visitor = new CypherVisitor();
            Node movie = Node.Create("Movie").Named("m");
            Match statement = new(false, new Pattern(movie), null);
            statement.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }
    }
}
