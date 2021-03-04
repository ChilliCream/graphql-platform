using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// A condition that consists of one or two
    /// </summary>
    public class CompoundCondition : Condition
    {
        public override ClauseKind Kind => ClauseKind.CompoundCondition;

        private static readonly CompoundCondition _emptyCondition =
            new (null);

        private static readonly HashSet<Operator> _validOperations =
            new() { Operator.And, Operator.Or, Operator.XOr };

        private readonly Operator _operator;
        private readonly List<Condition> _conditions;

        public new Condition And(Condition condition) {
            return Add(Operator.And, condition);
        }

        public static CompoundCondition Empty() {
            return _emptyCondition;
        }

        private CompoundCondition(Operator op)
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

            if (_operator != chainingOperator) return Create(this, chainingOperator, condition);
            _conditions.Add(condition);
            return this;
        }

        private bool CanBeFlattenedWith(Operator operatorBefore)
        {
            if (_operator != operatorBefore) {
                return false;
            }
            foreach (Condition c in _conditions)
            {
                if (c is CompoundCondition condition && condition._operator != operatorBefore)
                {
                    return false;
                }
            }
            return true;
        }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            // There is nothing to visit here
            if (!_conditions.Any())
            {
                return;
            }

            // Fold single condition
            var hasManyConditions = _conditions.Count > 1;
            if (hasManyConditions)
            {
                cypherVisitor.Enter(this);
            }

            // The first nested condition does not need an operator
            AcceptVisitorWithOperatorForChildCondition(cypherVisitor, null, _conditions[0]);

            // All others do
            if (!hasManyConditions) return;
            foreach (Condition condition in _conditions.Skip(1))
            {
                // This takes care of a potential inner compound condition that got added with a different operator
                // and thus forms a tree.
                Operator actualOperator = condition is CompoundCondition condition1 ?
                    condition1._operator :
                    _operator;
                AcceptVisitorWithOperatorForChildCondition(cypherVisitor, actualOperator, condition);
            }
            cypherVisitor.Leave(this);
        }

        private static void AcceptVisitorWithOperatorForChildCondition(
            CypherVisitor cypherVisitor, IVisitable op, IVisitable condition)
        {
            op?.Visit(cypherVisitor);
            condition.Visit(cypherVisitor);
        }
    }
}
