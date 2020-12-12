using System;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// A set of operations.
    /// </summary>
    public class Operations
    {
        static Operation Concat(Expression op1, Expression op2)
        {

            return Operation.Create(op1, Operator.Concat, op2);
        }

        static Operation Add(Expression op1, Expression op2)
        {

            return Operation.Create(op1, Operator.Addition, op2);
        }

        static Operation Subtract(Expression op1, Expression op2)
        {

            return Operation.Create(op1, Operator.Subtraction, op2);
        }

        static Operation Multiply(Expression op1, Expression op2)
        {

            return Operation.Create(op1, Operator.Multiplication, op2);
        }

        static Operation Divide(Expression op1, Expression op2)
        {

            return Operation.Create(op1, Operator.Division, op2);
        }

        static Operation Remainder(Expression op1, Expression op2)
        {

            return Operation.Create(op1, Operator.Modulo, op2);
        }

        static Operation Pow(Expression op1, Expression op2)
        {

            return Operation.Create(op1, Operator.Exponent, op2);
        }

        /**
		 * Creates a {@code =} operation. The left hand side should resolve to a property or to something which has labels
		 * or types to modify and the right hand side should either be new properties or labels.
		 *
		 * @param target The target that should be modified
		 * @param value  The new value of the target
		 * @return A new operation.
		 */
        static Operation Set(Expression target, Expression value)
        {

            return Operation.Create(target, Operator.Set, value);
        }

        //static Operation Set(Node target, string[] label)
        //{

        //    return Operation.Create(target, Operator.SetLabel, label);
        //}

        //static Operation Remove(Node target, string[] label)
        //{

        //    return Operation.Create(target, Operator.RemoveLabel, label);
        //}


        /**
		 * Not to be instantiated.
		 */
        private Operations() => throw new NotImplementedException();
    }
}
