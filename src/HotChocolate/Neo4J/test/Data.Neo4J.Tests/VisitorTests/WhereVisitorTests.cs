using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Neo4J.Language
{
    public class WhereVisitorTests
    {
        [Fact]
        public void Where()
        {
            var visitor = new CypherVisitor();
            Node movie = Node.Create("Movie").Named("m");

            Condition condition = movie.Property("Title").IsEqualTo(Cypher.LiteralOf("The Matrix"));
            Where where = new(condition);
            where.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void WhereStatementIncludingAndCompoundCondition()
        {
            var visitor = new CypherVisitor();
            Node movie = Node.Create("Movie").Named("m");

            Condition condition1 = movie.Property("Title").IsEqualTo(Cypher.LiteralOf("The Matrix"));
            Condition condition2 = movie.Property("Rating").IsEqualTo(Cypher.LiteralOf(3.2));
            Condition andCondition = Condition.And(condition1, condition2);
            Where where = new(andCondition);
            where.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void WhereStatementIncludingOrCompoundCondition()
        {
            var visitor = new CypherVisitor();
            Node movie = Node.Create("Movie").Named("m");

            Condition condition1 = movie.Property("Title").IsEqualTo(Cypher.LiteralOf("The Matrix"));
            Condition condition2 = movie.Property("Rating").IsEqualTo(Cypher.LiteralOf(3.2));
            Condition andCondition = Condition.Or(condition1, condition2);
            Where where = new(andCondition);
            where.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void WhereStatementIncludingXOrCompoundCondition()
        {
            var visitor = new CypherVisitor();
            Node movie = Node.Create("Movie").Named("m");

            Condition condition1 = movie.Property("Title").IsEqualTo(Cypher.LiteralOf("The Matrix"));
            Condition condition2 = movie.Property("Rating").IsEqualTo(Cypher.LiteralOf(3.2));
            Condition andCondition = Condition.XOr(condition1, condition2);
            Where where = new(andCondition);
            where.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void WhereStatementIncludingTwoAndCompoundCondition()
        {
            var visitor = new CypherVisitor();
            Node movie = Node.Create("Movie").Named("m");

            Node bike = Node.Create("Bike").Named("b");

            Condition condition1 = movie.Property("Title").IsEqualTo(Cypher.LiteralOf("The Matrix"));
            Condition condition2 = movie.Property("Rating").IsEqualTo(Cypher.LiteralOf(3.2));
            Condition condition3 = bike.Property("Broken").IsEqualTo(Cypher.LiteralTrue());
            Condition orCondition = Condition.Or(condition1, condition2);
            Condition andCondition = Condition.And(orCondition, condition3);
            Where where = new(andCondition);
            where.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void WhereStatementIncludingAndCompoundConditionChain()
        {
            var visitor = new CypherVisitor();
            Node movie = Node.Create("Movie").Named("m");

            Condition condition1 = movie.Property("Title").IsEqualTo(Cypher.LiteralOf("The Matrix"));
            Condition condition2 = movie.Property("Rating").IsEqualTo(Cypher.LiteralOf(3.2));
            Condition compoundCondition = condition1.And(condition2);
            Where where = new(compoundCondition);
            where.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void WhereStatementIncludingOrCompoundConditionChain()
        {
            var visitor = new CypherVisitor();
            Node movie = Node.Create("Movie").Named("m");

            Condition condition1 = movie.Property("Title").IsEqualTo(Cypher.LiteralOf("The Matrix"));
            Condition condition2 = movie.Property("Rating").IsEqualTo(Cypher.LiteralOf(3.2));
            Condition compoundCondition = condition1.Or(condition2);
            Where where = new(compoundCondition);
            where.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void WhereStatementIncludingXOrCompoundConditionChain()
        {
            var visitor = new CypherVisitor();
            Node movie = Node.Create("Movie").Named("m");

            Where where = new(
                movie.Property("Title").IsEqualTo(Cypher.LiteralOf("The Matrix"))
                    .XOr(movie.Property("Rating").IsEqualTo(Cypher.LiteralOf(3.2))));
            where.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void WhereStatementIncludingXOrCompoundConditionChainMutiple()
        {
            var visitor = new CypherVisitor();
            Node movie = Node.Create("Movie").Named("m");

            Where where = new(
                movie.Property("Title").IsEqualTo(Cypher.LiteralOf("The Matrix"))
                    .XOr(movie.Property("Rating").IsEqualTo(Cypher.LiteralOf(3.2))
                        .And(movie.Property("Age").IsEqualTo(Cypher.LiteralOf(2)))));
            where.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }

        [Fact]
        public void WhereStatementIncludingXOrLargeChain()
        {
            var visitor = new CypherVisitor();
            Node movie = Node.Create("Movie").Named("m");

            Condition property1 = movie.Property("Title").IsEqualTo(Cypher.LiteralOf("The Matrix"));
            Condition property2 = movie.Property("Rating").IsEqualTo(Cypher.LiteralOf(3.2));
            Condition property3 = movie.Property("Age").IsEqualTo(Cypher.LiteralOf(2));
            Condition property4 = movie.Property("Name").IsEqualTo(Cypher.LiteralOf("Peter"));
            Condition property5 = movie.Property("Name").IsEqualTo(Cypher.LiteralOf("Tim"));

            Where where = new(property1.XOr(property2.And(property3)).Or(property4.Or(property5).Not()));
            where.Visit(visitor);
            visitor.Print().MatchSnapshot();
        }
    }
}
