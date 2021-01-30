using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Data.Neo4J.Language
{
    public class CompoundCondition : Condition
    {
        public override ClauseKind Kind => ClauseKind.CompoundCondition;

        private static readonly CompoundCondition _emptyCondition =
            new (null);

        private static readonly HashSet<Operator> _validOperations =
            new() { Operator.And, Operator.Or, Operator.XOr };

        private readonly Operator? _operator;
        private readonly List<Condition> _conditions;

        public new Condition And(Condition condition) {
            return Add(Operator.And, condition);
        }

        static CompoundCondition Empty() {
            return _emptyCondition;
        }

        public CompoundCondition(Operator? op)
        {
            _operator = op;
            _conditions = new List<Condition>();
        }

        public static CompoundCondition Create(Condition left, Operator op, Condition right)
        {
            return new CompoundCondition(op).Add(op, left).Add(op, right);
        }


        private CompoundCondition Add(Operator chainingOperator, Condition condition)
        {
            if (this == _emptyCondition)
            {
                return new CompoundCondition(chainingOperator).Add(chainingOperator, condition);
            }

            if (condition == _emptyCondition)
            {
                return this;
            }

            if (condition is CompoundCondition compoundCondition)
            {
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
                    var inner = new CompoundCondition(chainingOperator);
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

        public override void Visit(CypherVisitor visitor)
        {
            if (!_conditions.Any())
            {
                return;
            }
            var hasManyConditions = _conditions.Count > 1;

            if (hasManyConditions)
            {
                visitor.Enter(this);
            }

            AcceptVisitorWithOperatorForChildCondition(visitor, null, _conditions[0]);

            if (!hasManyConditions) return;
            foreach (Condition condition in _conditions.Skip(1))
            {
                // This takes care of a potential inner compound condition that got added with a different operator
                // and thus forms a tree.
                Operator? actualOperator = condition is CompoundCondition condition1 ?
                    condition1._operator :
                    _operator;
                AcceptVisitorWithOperatorForChildCondition(visitor, actualOperator, condition);
            }
            visitor.Leave(this);
        }

        private static void AcceptVisitorWithOperatorForChildCondition(
            CypherVisitor visitor, IVisitable? op, IVisitable condition)
        {
            op?.Visit(visitor);
            condition.Visit(visitor);
        }
    }
}
