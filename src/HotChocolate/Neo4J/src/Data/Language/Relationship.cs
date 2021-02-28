using System;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// See <a href="https://s3.amazonaws.com/artifacts.opencypher.org/M15/railroad/RelationshipPattern.html">RelationshipPattern</a>.
    /// </summary>
    public class Relationship : IRelationshipPattern, IPropertyContainer, IExposesProperties<Relationship>
    {
        private readonly Node _left;
        private readonly Node _right;
        private readonly Details _details;

        public Relationship(Node left, Details details, Node right) {
            _left = left;
            _right = right;
            _details = details;
        }

        public static Relationship Create(
            Node left,
            RelationshipDirection relationshipDirection,
            Node right,
            params string[] types)
        {
            Ensure.IsNotNull(left, "Left node is required.");
            Ensure.IsNotNull(right, "Right node is required.");

            return new Relationship(left, new, right);
        }

        Node GetLeft() => _left;

        Node GetRight() => _right;


        public SymbolicName? GetSymbolicName()
        {
            throw new NotImplementedException();
        }

        public Property Property(string name)
        {
            throw new NotImplementedException();
        }

        public Relationship WithProperties(MapExpression newProps)
        {
            throw new NotImplementedException();
        }

        public Relationship WithProperties(params object[] keysAndValues)
        {
            throw new NotImplementedException();
        }
    }
}
