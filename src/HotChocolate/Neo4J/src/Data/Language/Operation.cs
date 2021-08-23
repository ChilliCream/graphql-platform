using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// A binary operation.
    /// </summary>
    public class Operation : Expression
    {
        private static readonly IReadOnlyList<Operator> _labelOperators =
            new[]
            {
                Operator.SetLabel,
                Operator.RemoveLabel
            };

        private static readonly IReadOnlyList<Operator> _dontGroup =
            new[]
            {
                Operator.Exponent,
                Operator.Pipe
            };

        private static readonly IReadOnlyList<OperatorType> _needsGroupingByType =
            new[]
            {
                OperatorType.Property,
                OperatorType.Label
            };

        public Operation(Expression left, Operator @operator, Expression right)
        {
            Left = left;
            Operator = @operator;
            Right = right;
        }

        public Operation(Expression left, Operator @operator, NodeLabels right)
        {
            Left = left;
            Operator = @operator;
            Right = right;
        }

        public override ClauseKind Kind => ClauseKind.Operation;

        public Expression Left { get; }

        public Operator Operator { get; }

        public Visitable Right { get; }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            Expressions.NameOrExpression(Left).Visit(cypherVisitor);
            Operator.Visit(cypherVisitor);
            Right.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }

        public bool NeedsGrouping() =>
            _needsGroupingByType.Contains(Operator.Type) && !_dontGroup.Contains(Operator);

        public static Operation Create(Expression op1, Operator op, Expression op2)
        {
            Ensure.IsNotNull(op1, "The first operand must not be null.");
            Ensure.IsNotNull(op, "Operator must not be empty.");
            Ensure.IsNotNull(op2, "The second operand must not be null.");

            return new Operation(op1, op, op2);
        }

        public static Operation Create(Expression op1, Operator op, params string[] nodeLabels)
        {
            Ensure.IsNotNull(op1, "The first operand must not be null.");
            Ensure.IsTrue(_labelOperators.Contains(op),
                "Only operators can be used to modify labels.");
            Ensure.IsNotEmpty(nodeLabels, "The labels cannot be empty.");

            return new Operation(
                op1,
                op,
                new NodeLabels(nodeLabels.Select(x => new NodeLabel(x)).ToList()));
        }
    }
}
