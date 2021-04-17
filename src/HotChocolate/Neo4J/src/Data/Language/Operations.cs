using System;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// A set of operations.
    /// </summary>
    public static class Operations
    {
        public static Operation Concat(Expression op1, Expression op2) =>
            Operation.Create(op1, Operator.Concat, op2);

        public static Operation Add(Expression op1, Expression op2) =>
            Operation.Create(op1, Operator.Addition, op2);


        public static Operation Subtract(Expression op1, Expression op2) =>
            Operation.Create(op1, Operator.Subtraction, op2);

        public static Operation Multiply(Expression op1, Expression op2) =>
            Operation.Create(op1, Operator.Multiplication, op2);

        public static Operation Divide(Expression op1, Expression op2) =>
            Operation.Create(op1, Operator.Division, op2);

        public static Operation Remainder(Expression op1, Expression op2) =>
            Operation.Create(op1, Operator.Modulo, op2);

        public static Operation Pow(Expression op1, Expression op2) =>
            Operation.Create(op1, Operator.Exponent, op2);

        public static Operation Set(Expression target, Expression value) =>
            Operation.Create(target, Operator.Set, value);
    }
}
