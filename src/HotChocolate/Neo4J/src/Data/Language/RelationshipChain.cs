using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Represents a chain of relationships. The chain is meant to be in order and the right node of an element is related to
    /// the left node of the next element.
    /// </summary>
    public class RelationshipChain : IRelationshipPattern
    {
        public ClauseKind Kind { get; } = ClauseKind.RelationshipChain;
        private readonly LinkedList<Relationship> _relationships = new();

        private RelationshipChain() { }

        public static RelationshipChain Create(Relationship firstElement) =>
            new RelationshipChain().Add(firstElement);

        private RelationshipChain Add(Relationship element)
        {
            Ensure.IsNotNull(element, "Elements of a relationship chain must not be null.");
            _relationships.AddLast(element);
            return this;
        }

        public RelationshipChain RelationshipTo(Node other, params string[] types)
        {
            this._relationships.AddLast(_relationships.RemoveLast().GetRight().RelationshipTo(other, types));
        }

        public RelationshipChain RelationshipFrom(Node other, params string[] types)
        {
            throw new System.NotImplementedException();
        }

        public RelationshipChain RelationshipBetween(Node other, params string[] types)
        {
            throw new System.NotImplementedException();
        }

        public IExposesRelationships<RelationshipChain> Named(string name)
        {
            throw new System.NotImplementedException();
        }

        public Condition AsCondition()
        {
            throw new System.NotImplementedException();
        }

        public void Visit(CypherVisitor cypherVisitor)
        {
            throw new System.NotImplementedException();
        }
    }
}
