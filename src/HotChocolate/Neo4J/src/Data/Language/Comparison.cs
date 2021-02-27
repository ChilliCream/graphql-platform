﻿namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// A concrete condition representing a comparision between two expressions.
    /// </summary>
    public class Comparison : Condition
    {
        public override ClauseKind Kind => ClauseKind.Comparison;
        private readonly Expression _left;
        private readonly Operator _operator;
        private readonly Expression _right;

        private Comparison(Expression left, Operator op, Expression right)
        {
            _left = NestedIfCondition(left);
            _operator = op;
            _right = NestedIfCondition(right);
        }

        public static Comparison Create(Expression left, Operator op, Expression right) =>
            new(left, op, right);

        public static Comparison Create(Operator op, Expression expression) => op.GetType()
            switch
            {
                Operator.Type.Prefix => new Comparison(null, op, expression),
                Operator.Type.Postfix => new Comparison(expression, op, null),
                _ => throw new System.ArgumentException("Invalid operator type"),
            };

        private static Expression NestedIfCondition(Expression expression) =>
            expression is Condition ? new NestedExpression(expression) : expression;

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            if (_left != null)
                Expressions.NameOrExpression(_left).Visit(cypherVisitor);
            _operator.Visit(cypherVisitor);
            if (_right != null)
                Expressions.NameOrExpression(_right).Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
