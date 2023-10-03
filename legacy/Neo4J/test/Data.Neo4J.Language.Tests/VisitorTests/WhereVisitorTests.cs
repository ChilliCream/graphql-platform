using CookieCrumble;
using HotChocolate.Data.Neo4J.Language;

namespace HotChocolate.Data.VisitorTests;

public class WhereVisitorTests
{
    [Fact]
    public void Where()
    {
        var visitor = new CypherVisitor();
        var movie = Node.Create("Movie").Named("m");

        var condition = movie.Property("Title").IsEqualTo(Cypher.LiteralOf("The Matrix"));
        Where where = new(condition);
        where.Visit(visitor);
        visitor.Print().MatchSnapshot();
    }

    [Fact]
    public void WhereStatementIncludingAndCompoundCondition()
    {
        var visitor = new CypherVisitor();
        var movie = Node.Create("Movie").Named("m");

        var condition1 =
            movie.Property("Title").IsEqualTo(Cypher.LiteralOf("The Matrix"));
        var condition2 = movie.Property("Rating").IsEqualTo(Cypher.LiteralOf(3.2));
        var andCondition = Condition.And(condition1, condition2);
        Where where = new(andCondition);
        where.Visit(visitor);
        visitor.Print().MatchSnapshot();
    }

    [Fact]
    public void WhereStatementIncludingOrCompoundCondition()
    {
        var visitor = new CypherVisitor();
        var movie = Node.Create("Movie").Named("m");

        var condition1 =
            movie.Property("Title").IsEqualTo(Cypher.LiteralOf("The Matrix"));
        var condition2 = movie.Property("Rating").IsEqualTo(Cypher.LiteralOf(3.2));
        var andCondition = Condition.Or(condition1, condition2);
        Where where = new(andCondition);
        where.Visit(visitor);
        visitor.Print().MatchSnapshot();
    }

    [Fact]
    public void WhereStatementIncludingXOrCompoundCondition()
    {
        var visitor = new CypherVisitor();
        var movie = Node.Create("Movie").Named("m");

        var condition1 =
            movie.Property("Title").IsEqualTo(Cypher.LiteralOf("The Matrix"));
        var condition2 = movie.Property("Rating").IsEqualTo(Cypher.LiteralOf(3.2));
        var andCondition = Condition.XOr(condition1, condition2);
        Where where = new(andCondition);
        where.Visit(visitor);
        visitor.Print().MatchSnapshot();
    }

    [Fact]
    public void WhereStatementIncludingTwoAndCompoundCondition()
    {
        var visitor = new CypherVisitor();
        var movie = Node.Create("Movie").Named("m");

        var bike = Node.Create("Bike").Named("b");

        var condition1 =
            movie.Property("Title").IsEqualTo(Cypher.LiteralOf("The Matrix"));
        var condition2 = movie.Property("Rating").IsEqualTo(Cypher.LiteralOf(3.2));
        var condition3 = bike.Property("Broken").IsEqualTo(Cypher.LiteralTrue());
        var orCondition = Condition.Or(condition1, condition2);
        var andCondition = Condition.And(orCondition, condition3);
        Where where = new(andCondition);
        where.Visit(visitor);
        visitor.Print().MatchSnapshot();
    }

    [Fact]
    public void WhereStatementIncludingAndCompoundConditionChain()
    {
        var visitor = new CypherVisitor();
        var movie = Node.Create("Movie").Named("m");

        var condition1 =
            movie.Property("Title").IsEqualTo(Cypher.LiteralOf("The Matrix"));
        var condition2 = movie.Property("Rating").IsEqualTo(Cypher.LiteralOf(3.2));
        var compoundCondition = condition1.And(condition2);
        Where where = new(compoundCondition);
        where.Visit(visitor);
        visitor.Print().MatchSnapshot();
    }

    [Fact]
    public void WhereStatementIncludingOrCompoundConditionChain()
    {
        var visitor = new CypherVisitor();
        var movie = Node.Create("Movie").Named("m");

        var condition1 =
            movie.Property("Title").IsEqualTo(Cypher.LiteralOf("The Matrix"));
        var condition2 = movie.Property("Rating").IsEqualTo(Cypher.LiteralOf(3.2));
        var compoundCondition = condition1.Or(condition2);
        Where where = new(compoundCondition);
        where.Visit(visitor);
        visitor.Print().MatchSnapshot();
    }

    [Fact]
    public void WhereStatementIncludingXOrCompoundConditionChain()
    {
        var visitor = new CypherVisitor();
        var movie = Node.Create("Movie").Named("m");

        Where where = new(
            movie.Property("Title")
                .IsEqualTo(Cypher.LiteralOf("The Matrix"))
                .XOr(movie.Property("Rating").IsEqualTo(Cypher.LiteralOf(3.2))));
        where.Visit(visitor);
        visitor.Print().MatchSnapshot();
    }

    [Fact]
    public void WhereStatementIncludingXOrCompoundConditionChainMutiple()
    {
        var visitor = new CypherVisitor();
        var movie = Node.Create("Movie").Named("m");

        Where where = new(
            movie.Property("Title")
                .IsEqualTo(Cypher.LiteralOf("The Matrix"))
                .XOr(movie.Property("Rating")
                    .IsEqualTo(Cypher.LiteralOf(3.2))
                    .And(movie.Property("Age").IsEqualTo(Cypher.LiteralOf(2)))));
        where.Visit(visitor);
        visitor.Print().MatchSnapshot();
    }

    [Fact]
    public void WhereStatementIncludingXOrLargeChain()
    {
        var visitor = new CypherVisitor();
        var movie = Node.Create("Movie").Named("m");

        var property1 = movie.Property("Title").IsEqualTo(Cypher.LiteralOf("The Matrix"));
        var property2 = movie.Property("Rating").IsEqualTo(Cypher.LiteralOf(3.2));
        var property3 = movie.Property("Age").IsEqualTo(Cypher.LiteralOf(2));
        var property4 = movie.Property("Name").IsEqualTo(Cypher.LiteralOf("Peter"));
        var property5 = movie.Property("Name").IsEqualTo(Cypher.LiteralOf("Tim"));

        Where where = new(property1.XOr(property2.And(property3))
            .Or(property4.Or(property5).Not()));
        where.Visit(visitor);
        visitor.Print().MatchSnapshot();
    }
}
