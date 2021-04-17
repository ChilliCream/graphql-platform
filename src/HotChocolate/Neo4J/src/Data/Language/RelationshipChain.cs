using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Represents a chain of relationships. The chain is meant to be in order and the right node of an element is related to
    /// the left node of the next element.
    /// </summary>
    public class RelationshipChain
        : Visitable
            , IRelationshipPattern
    {
        public override ClauseKind Kind { get; } = ClauseKind.RelationshipChain;
        private readonly List<Relationship> _relationships = new();

        public static RelationshipChain Create(Relationship firstElement) =>
            new RelationshipChain().Add(firstElement);

        public RelationshipChain Add(Relationship element)
        {
            Ensure.IsNotNull(element, "Elements of a relationship chain must not be null.");
            _relationships.Add(element);
            return this;
        }

        public RelationshipChain RelationshipTo(Node other, params string[] types)
        {
            return Add(_relationships.Peek().GetRight().RelationshipTo(other, types));
        }

        public RelationshipChain RelationshipFrom(Node other, params string[] types)
        {
            return Add(_relationships.Peek().GetRight().RelationshipFrom(other, types));
        }

        public RelationshipChain RelationshipBetween(Node other, params string[] types)
        {
            return Add(_relationships.Peek().GetRight().RelationshipBetween(other, types));
        }

        public RelationshipChain Unbounded()
        {
            Relationship lastElement = _relationships.Pop();
            return Add(lastElement.Unbounded());
        }

        public RelationshipChain Minmum(int minimum)
        {
            Relationship lastElement = _relationships.Pop();
            return Add(lastElement.Minimum(minimum));
        }

        public RelationshipChain Maximum(int maximum)
        {
            Relationship lastElement = _relationships.Pop();
            return Add(lastElement.Maximum(maximum));
        }

        public RelationshipChain Length(int minimum, int maximum)
        {
            Relationship lastElement = _relationships.Pop();
            return Add(lastElement.Length(minimum, maximum));
        }

        public RelationshipChain Properties(MapExpression newpProperties)
        {
            Relationship lastElement = _relationships.Pop();
            return Add(lastElement.WithProperties(newpProperties));
        }

        public RelationshipChain Properties(params object[] keysAndValues)
        {
            Relationship lastElement = _relationships.Pop();
            return Add(lastElement.WithProperties(keysAndValues));
        }

        public RelationshipChain Named(string newSymbolicName)
        {
            Relationship lastElement = _relationships.Pop();
            return Add(lastElement.Named(newSymbolicName));
        }

        public Condition AsCondition() => new RelationshipPatternCondition(this);

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            Node lastNode = null;
            foreach (Relationship relationship in _relationships)
            {
                relationship.GetLeft().Visit(cypherVisitor);
                relationship.GetDetails().Visit(cypherVisitor);

                lastNode = relationship.GetRight();
            }
            lastNode?.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
