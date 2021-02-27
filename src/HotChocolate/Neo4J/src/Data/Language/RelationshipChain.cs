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
        public void Visit(CypherVisitor cypherVisitor)
        {
            throw new System.NotImplementedException();
        }

        private readonly LinkedList<Relationship> _relationships = new();

        public RelationshipChain() { }

        public static RelationshipChain Create(Relationship firstElement) =>
            new RelationshipChain().Add(firstElement);

        RelationshipChain Add(Relationship element)
        {
            _relationships.AddLast(element);
            return this;
        }

        public RelationshipChain RelationshipTo(Node other, params string[] types)
        {
            throw new System.NotImplementedException();
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
    }
}
