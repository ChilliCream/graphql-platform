using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// See
    /// <a href="https://s3.amazonaws.com/artifacts.opencypher.org/M15/railroad/RelationshipPattern.html">
    /// Relationship Pattern
    /// </a>
    /// </summary>
    public class Relationship
        : Visitable
        , IRelationshipPattern
        , IPropertyContainer
        , IExposesProperties<Relationship>
    {
        private Relationship(Node left, RelationshipDetails details, Node right)
        {
            Left = left;
            Right = right;
            Details = details;
        }

        public override ClauseKind Kind => ClauseKind.Relationship;

        public Node Left { get; }

        public Node Right { get; }

        public RelationshipDetails Details { get; }

        public Relationship Named(SymbolicName newSymbolicName) =>
            new(Left, Details.Named(newSymbolicName), Right);

        public Relationship Named(string newSymbolicName) =>
            new(Left, Details.Named(newSymbolicName), Right);

        public Relationship Unbounded() =>
            new(Left, Details.Unbounded(), Right);

        public Relationship Minimum(int minimum) =>
            new(Left, Details.Minimum(minimum), Right);

        public Relationship Maximum(int maximum) =>
            new(Left, Details.Maximum(maximum), Right);

        public Relationship Length(int minimum, int maximum) =>
            new(Left, Details.Minimum(minimum).Maximum(maximum), Right);

        public SymbolicName? SymbolicName => Details.SymbolicName;

        public SymbolicName RequiredSymbolicName => Details.SymbolicName;

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
            return SymbolicName.Project(entries);
        }

        public Relationship WithProperties(MapExpression? newProperties)
        {
            if (newProperties == null && Details.Properties == null)
            {
                return this;
            }

            return new Relationship(
                Left,
                Details.With(newProperties == null ? null : new Properties(newProperties)),
                Right);
        }

        public Relationship WithProperties(params object[] keysAndValues)
        {
            MapExpression? newProperties = null;
            if (keysAndValues != null && keysAndValues.Length != 0)
            {
                newProperties = MapExpression.Create(keysAndValues);
            }

            return WithProperties(newProperties);
        }

        public RelationshipChain RelationshipTo(Node other, params string[] types) =>
            RelationshipChain.Create(this).Add(Right.RelationshipTo(other, types));

        public RelationshipChain RelationshipFrom(Node other, params string[] types) =>
            RelationshipChain.Create(this).Add(Right.RelationshipFrom(other, types));

        public RelationshipChain RelationshipBetween(Node other, params string[] types) =>
            RelationshipChain.Create(this).Add(Right.RelationshipBetween(other, types));

        public Condition AsCondition() => new RelationshipPatternCondition(this);

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            Left.Visit(cypherVisitor);
            Details.Visit(cypherVisitor);
            Right.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
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

            return new Relationship(left, details, right);
        }
    }
}
