using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack;

#nullable enable

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// See <a href="https://s3.amazonaws.com/artifacts.opencypher.org/M15/railroad/RelationshipPattern.html">RelationshipPattern</a>.
    /// </summary>
    public class Relationship :
        Visitable,
        IRelationshipPattern,
        IPropertyContainer,
        IExposesProperties<Relationship>
    {
        public override ClauseKind Kind => ClauseKind.Relationship;
        private readonly Node _left;
        private readonly Node _right;
        private readonly RelationshipDetails _details;

        private Relationship(Node left, RelationshipDetails details, Node right)
        {
            _left = left;
            _right = right;
            _details = details;
        }

        public static Relationship Create(
            Node left,
            RelationshipDirection? relationshipDirection,
            Node right,
            params string[] types)
        {
            Ensure.IsNotNull(left, "Left node is required.");
            Ensure.IsNotNull(right, "Right node is required.");

            var listOfTypes = types.Where(s => !string.IsNullOrEmpty(s)).ToList();

            var details = RelationshipDetails.Create(
                relationshipDirection ?? RelationshipDirection.None,
                null,
                listOfTypes.IsNullOrEmpty() ? null : new RelationshipTypes(listOfTypes));

            return new Relationship(left, details , right);
        }

        public Node GetLeft() => _left;

        public Node GetRight() => _right;

        public RelationshipDetails GetDetails() => _details;

        public Relationship Named(SymbolicName newSymbolicName) =>
            new (_left, _details.Named(newSymbolicName), _right);

        public Relationship Named(string newSymbolicName) =>
            new (_left, _details.Named(newSymbolicName), _right);



        public Relationship Unbounded() =>
            new (_left, _details.Unbounded(), _right);

        public Relationship Minimum(int minimum) =>
            new(_left, _details.Minimum(minimum), _right);

        public Relationship Maximum(int maximum) =>
            new(_left, _details.Maximum(maximum), _right);

        public Relationship Length(int minimum, int maximum) =>
            new(_left, _details.Minimum(minimum).Maximum(maximum), _right);

        public SymbolicName? GetSymbolicName() => _details.GetSymbolicName();


        public SymbolicName? GetRequiredSymbolicName()
        {
            return _details.GetSymbolicName();
        }

        public Property Property(string name)
        {
            throw new NotImplementedException();
        }

        public Property Property(params string[] names)
        {
            throw new NotImplementedException();
        }

        public Property Property(Expression lookup)
        {
            throw new NotImplementedException();
        }

        public MapProjection Project(List<object> entries)
        {
            return Project(entries.ToArray());
        }

        public MapProjection Project(params object[] entries)
        {
            return GetSymbolicName().Project(entries);
        }

        public Relationship WithProperties(MapExpression? newProperties)
        {
            if (newProperties == null && _details.GetProperties() == null)
            {
                return this;
            }

            return new Relationship(
                _left,
                _details.With(newProperties == null ? null : new Properties(newProperties)),
                _right);
        }

        public Relationship WithProperties(params object[] keysAndValues)
        {
            MapExpression newProperties = null;
            if (keysAndValues != null && keysAndValues.Length != 0)
            {
                newProperties = MapExpression.Create(keysAndValues);
            }

            return WithProperties(newProperties);
        }

        public RelationshipChain RelationshipTo(Node other, params string[] types) =>
            RelationshipChain.Create(this).Add(_right.RelationshipTo(other, types));

        public RelationshipChain RelationshipFrom(Node other, params string[] types) =>
            RelationshipChain.Create(this).Add(_right.RelationshipFrom(other, types));

        public RelationshipChain RelationshipBetween(Node other, params string[] types) =>
            RelationshipChain.Create(this).Add(_right.RelationshipBetween(other, types));

        public Condition AsCondition() => new RelationshipPatternCondition(this);

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _left.Visit(cypherVisitor);
            _details.Visit(cypherVisitor);
            _right.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
