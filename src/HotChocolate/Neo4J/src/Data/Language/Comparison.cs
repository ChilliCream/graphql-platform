namespace HotChocolate.Data.Neo4J.Language;

/// <summary>
/// A concrete condition representing a comparision between two expressions.
/// </summary>
public class Comparison : Condition
{
    private Comparison(Expression? left, Operator op, Expression? right)
    {
        Left = NestedIfCondition(left);
        Operator = op;
        Right = NestedIfCondition(right);
    }

    public override ClauseKind Kind => ClauseKind.Comparison;

    public Expression? Left { get; }

    public Operator Operator { get; }

    public Expression? Right { get; }

    public override void Visit(CypherVisitor cypherVisitor)
    {
        cypherVisitor.Enter(this);

        if (Left is not null)
        {
            Expressions.NameOrExpression(Left).Visit(cypherVisitor);
        }

        Operator.Visit(cypherVisitor);

        if (Right is not null)
        {
            Expressions.NameOrExpression(Right).Visit(cypherVisitor);
        }

        cypherVisitor.Leave(this);
    }

    public static Comparison Create(Expression left, Operator op, Expression right) =>
        new(left, op, right);

    public static Comparison Create(Operator op, Expression expression) => op.Type
        switch
        {
            OperatorType.Prefix => new Comparison(null, op, expression),
            OperatorType.Postfix => new Comparison(expression, op, null),
            _ => throw new ArgumentException("Invalid operator type"),
        };

    private static Expression? NestedIfCondition(Expression? expression) =>
        expression is Condition ? new NestedExpression(expression) : expression;
}
