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
        /**
	 * A set of operators triggering operations on labels.
	 */
        private readonly static EnumSet<Operator> LABEL_OPERATORS = EnumSet.of(Operator.SET_LABEL, Operator.REMOVE_LABEL);
        private static final EnumSet<Operator.Type> NEEDS_GROUPING_BY_TYPE = EnumSet
            .complementOf(EnumSet.of(Operator.Type.PROPERTY, Operator.Type.LABEL));
        private static final EnumSet<Operator> DONT_GROUP = EnumSet.of(Operator.EXPONENTIATION, Operator.PIPE);

        static Operation Create(Expression op1, Operator op, Expression op2)
        {
            _ = op1 ?? throw new ArgumentNullException(nameof(op1));
            _ = op ?? throw new ArgumentNullException(nameof(op));
            _ = op2 ?? throw new ArgumentNullException(nameof(op2));

            return new Operation(op1, op, op2);
        }

        static Operation create(Node op1, Operator op, string[] nodeLabels)
        {

            List<NodeLabel> listOfNodeLabels = Arrays.stream(nodeLabels).map(NodeLabel::new).collect(Collectors.toList());
            return new Operation(op1.getRequiredSymbolicName(), op, new NodeLabels(listOfNodeLabels));
        }



        public Operation(Expression left, Operator op, Expression right)
        {

            _left = left;
            _op = op;
            _right = right;
        }

        public Operation(Expression left, Operator op, NodeLabels right)
        {

            this.left = left;
            this.operator = operator;
            this.right = right;
        }

        @Override
    public void accept(Visitor visitor)
        {

            visitor.enter(this);
            Expressions.nameOrExpression(left).accept(visitor);
		operator.accept(visitor);
            right.accept(visitor);
            visitor.leave(this);
        }

        /**
		 * Checks, whether this operation needs grouping.
		 *
		 * @return True, if this operation needs grouping.
		 */
        public boolean needsGrouping()
        {
            return NEEDS_GROUPING_BY_TYPE.contains(this.operator.getType()) && !DONT_GROUP.contains(this.operator);
        }

    }
}