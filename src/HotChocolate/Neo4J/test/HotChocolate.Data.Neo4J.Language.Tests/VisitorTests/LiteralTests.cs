using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Neo4J.Language
{
    public class LiteralTests
    {
        [Fact]
        public void StringLiteral()
        {
            var visitor = new CypherVisitor();

            StringLiteral literal = new ("Test");
            literal.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void BooleanLiteral()
        {
            var visitor = new CypherVisitor();

            BooleanLiteral literal = Language.BooleanLiteral.True;
            literal.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void IntegerLiteral()
        {
            var visitor = new CypherVisitor();

            IntegerLiteral literal = new (1);
            literal.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void DoubleLiteral()
        {
            var visitor = new CypherVisitor();

            DoubleLiteral literal = new (1.11);
            literal.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }
    }
}
