using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    public class CompoundCondition : Condition
    {
        public readonly static CompoundCondition EmptyCondition =
            new CompoundCondition(null);
        public readonly static HashSet<Operator> ValidOperations =
            new HashSet<Operator> { Operator.And, Operator.Or, Operator.XOr };


        private readonly Operator _operator;
        private readonly List<Condition> _conditions;

        public CompoundCondition(Operator op)
        {
            _operator = op;
            _conditions = new List<Condition>();
        }

        public static CompoundCondition Create(Condition left, Operator op, Condition right) =>
            new CompoundCondition(op).Add(op, left).Add(op, right);

        public static CompoundCondition Empty => EmptyCondition;

        private CompoundCondition Add(Operator chainingOperator, Condition condition)
        {
            if (this == EmptyCondition)
            {
                return new CompoundCondition(chainingOperator).Add(chainingOperator, condition);
            }

            if (condition == EmptyCondition)
            {
                return this;
            }

            if (condition is CompoundCondition)
            {
                CompoundCondition compoundCondition = (CompoundCondition)condition;

                if (_operator == chainingOperator && chainingOperator == compoundCondition._operator)
                {
                    if (compoundCondition.CanBeFlattenedWith(chainingOperator))
                    {
                        _conditions.AddRange(compoundCondition._conditions);
                    }
                    else
                    {
                        _conditions.Add(compoundCondition);
                    }
                }
                else
                {
                    CompoundCondition inner = new CompoundCondition(chainingOperator);
                    inner._conditions.Add(compoundCondition);
                    _conditions.Add(inner);
                }

                return this;
            }

            if (_operator == chainingOperator)
            {
                _conditions.Add(condition);
                return this;
            }

            return Create(this, chainingOperator, condition);
        }

        private bool CanBeFlattenedWith(Operator operatorBefore)
        {

            foreach (Condition c in _conditions)
            {
                if (c is CompoundCondition condition && condition._operator != operatorBefore)
                {
                    return false;
                }
            }
            return true;
        }

        public new void Visit(CypherVisitor visitor)
        {
            if (_conditions.Count == 0)
            {
                return;
            }
            bool hasManyConditions = _conditions.Count > 1;

            if (hasManyConditions)
            {
                visitor.Enter(this);
            }

            AcceptVisitorWithOperatorForChildCondition(visitor, null, _conditions[0]);

            if (hasManyConditions)
            {
                foreach (Condition condition in _conditions.GetRange(1, _conditions.Count))
                {
                    // This takes care of a potential inner compound condition that got added with a different operator
                    // and thus forms a tree.
                    Operator actualOperator = condition is CompoundCondition ?

                    ((CompoundCondition)condition)._operator :
                    _operator;
                    AcceptVisitorWithOperatorForChildCondition(visitor, actualOperator, condition);
                }
                visitor.Leave(this);
            }

        }

        private static void AcceptVisitorWithOperatorForChildCondition(
            CypherVisitor visitor, Operator op, Condition condition
            )
        {
            VisitIfNotNull(op, visitor);
            condition.Visit(visitor);
        }
    }
}