using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Neo4J.Language;

public class CypherWriteTests
{
    private static readonly Node _bikeNode = Cypher.Node("Bike").Named("b");
    private static readonly Node _userNode = Cypher.Node("User").Named("u");

    [Fact]
    public void MatchThreeNodes()
    {
        StatementBuilder statement = Cypher
            .Match(_bikeNode, _userNode, Cypher.Node("U").Named("o"));
        statement.Build().MatchSnapshot();
    }
}
