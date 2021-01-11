using HotChocolate.Data.Neo4J.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Neo4J.Tests
{
    public class ExpressionVisitorTests
    {
        [Fact]
        public void ExpressionIsEqualTo()
        {
            var visitor = new CypherVisitor();
            Node movie = Node.Create("Movie").Named("m");

            Condition statement = movie.Property("Title").IsEqualTo(Cypher.LiteralOf("The Matrix"));
            statement.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void ExpressionNotIsEqualTo()
        {
            var visitor = new CypherVisitor();
            Node movie = Node.Create("Movie").Named("m");

            Condition statement = movie.Property("Title").IsEqualTo(Cypher.LiteralOf("The Matrix")).Not();
            statement.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void ExpressionIsNotEqualTo()
        {
            var visitor = new CypherVisitor();
            Node movie = Node.Create("Movie").Named("m");

            Condition statement = movie.Property("Title").IsNotEqualTo(Cypher.LiteralOf("The Matrix"));
            statement.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void ExpressionLessThan()
        {
            var visitor = new CypherVisitor();
            Node movie = Node.Create("Movie").Named("m");

            Condition statement = movie.Property("Title").LessThan(Cypher.LiteralOf("The Matrix"));
            statement.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void ExpressionLessThanOrEqualTo()
        {
            var visitor = new CypherVisitor();
            Node movie = Node.Create("Movie").Named("m");

            Condition statement = movie.Property("Title").LessThanOrEqualTo(Cypher.LiteralOf("The Matrix"));
            statement.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void ExpressionGreaterThan()
        {
            var visitor = new CypherVisitor();
            Node movie = Node.Create("Movie").Named("m");

            Condition statement = movie.Property("Title").GreaterThan(Cypher.LiteralOf("The Matrix"));
            statement.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void ExpressionGreaterThanOEqualTo()
        {
            var visitor = new CypherVisitor();
            Node movie = Node.Create("Movie").Named("m");

            Condition statement = movie.Property("Title").GreaterThanOEqualTo(Cypher.LiteralOf("The Matrix"));
            statement.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void ExpressionIsTrue()
        {
            var visitor = new CypherVisitor();
            Node movie = Node.Create("Movie").Named("m");

            Condition statement = movie.Property("Released").IsTrue();
            statement.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void ExpressionIsFalse()
        {
            var visitor = new CypherVisitor();
            Node movie = Node.Create("Movie").Named("m");

            Condition statement = movie.Property("Released").IsFalse();
            statement.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void ExpressionMatches()
        {
            var visitor = new CypherVisitor();
            Node person = Node.Create("Person").Named("p");

            Condition statement = person.Property("Email").Matches(Cypher.LiteralOf("email@gmail.com"));
            statement.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }


        [Fact]
        public void ExpressionMatchesRegex()
        {
            var visitor = new CypherVisitor();
            Node person = Node.Create("Person").Named("p");

            Condition statement = person.Property("Email").Matches(".*\\\\.com");
            statement.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void ExpressionStartsWith()
        {
            var visitor = new CypherVisitor();
            Node movie = Node.Create("Movie").Named("m");

            Condition statement = movie.Property("Title").StartsWith(Cypher.LiteralOf("The"));
            statement.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void ExpressionEndsWith()
        {
            var visitor = new CypherVisitor();
            Node movie = Node.Create("Movie").Named("m");

            Condition statement = movie.Property("Title").EndsWith(Cypher.LiteralOf("Matrix"));
            statement.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void ExpressionContains()
        {
            var visitor = new CypherVisitor();
            Node movie = Node.Create("Movie").Named("m");

            Condition statement = movie.Property("Title").Contains(Cypher.LiteralOf("The"));
            statement.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void ExpressionIsNull()
        {
            var visitor = new CypherVisitor();
            Node movie = Node.Create("Movie").Named("m");

            Condition statement = movie.Property("Title").IsNull();
            statement.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void ExpressionIsNotNull()
        {
            var visitor = new CypherVisitor();
            Node movie = Node.Create("Movie").Named("m");

            Condition statement = movie.Property("Title").IsNotNull();
            statement.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }
    }
}
