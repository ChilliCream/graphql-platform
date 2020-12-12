using System;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// A binary operation.
    /// </summary>
    public class Operation : Expression
    {

        private readonly Expression _left;
        private readonly Operator _op;
        private readonly Visitable _right;


        public static Operation Create(Expression op1, Operator op, Expression op2)
        {
            _ = op1 ?? throw new ArgumentNullException(nameof(op1));
            _ = op ?? throw new ArgumentNullException(nameof(op));
            _ = op2 ?? throw new ArgumentNullException(nameof(op2));

            return new Operation(op1, op, op2);
        }

        public Operation(Expression left, Operator op, Expression right)
        {

            _left = left;
            _op = op;
            _right = right;
        }
    }
}
