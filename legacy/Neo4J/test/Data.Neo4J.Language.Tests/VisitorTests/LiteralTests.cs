using CookieCrumble;
using HotChocolate.Data.Neo4J.Language;

namespace HotChocolate.Data.VisitorTests;

public class LiteralTests
{
    [Fact]
    public void StringLiteral()
    {
        var visitor = new CypherVisitor();

        StringLiteral literal = new("Test");
        literal.Visit(visitor);
        visitor.Print().MatchSnapshot();
    }

    [Fact]
    public void BooleanLiteral()
    {
        var visitor = new CypherVisitor();

        var literal = Neo4J.Language.BooleanLiteral.True;
        literal.Visit(visitor);
        visitor.Print().MatchSnapshot();
    }

    [Fact]
    public void IntegerLiteral()
    {
        var visitor = new CypherVisitor();

        IntegerLiteral literal = new(1);
        literal.Visit(visitor);
        visitor.Print().MatchSnapshot();
    }

    [Fact]
    public void DoubleLiteral()
    {
        var visitor = new CypherVisitor();

        DoubleLiteral literal = new(1.11);
        literal.Visit(visitor);
        visitor.Print().MatchSnapshot();
    }
}
