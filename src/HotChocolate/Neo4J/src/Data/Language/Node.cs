using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// See
    /// <a href="https://s3.amazonaws.com/artifacts.opencypher.org/railroad/NodePattern.html">
    /// Node Pattern
    /// </a>
    /// </summary>
    public class Node
        : Visitable
        , IPatternElement
        , IPropertyContainer
        , IExposesRelationships<Relationship>
        , IExposesProperties<Node>
    {
        private Node(
            string? primaryLabel,
            Properties? properties,
            params string[]? additionalLabels)
        {
            SymbolicName = null;
            RequiredSymbolicName = null;
            Labels = new List<NodeLabel>();

            if (!string.IsNullOrEmpty(primaryLabel))
            {
                Labels.Add(new NodeLabel(primaryLabel));
            }

            if (additionalLabels != null && additionalLabels.Any())
            {
                foreach (var nodeLabel in additionalLabels)
                {
                    Labels.Add(new NodeLabel(nodeLabel));
                }
            }

            Properties = properties;
        }

        private Node(SymbolicName? symbolicName, Properties? properties, List<NodeLabel>? labels)
        {
            SymbolicName = symbolicName;
            RequiredSymbolicName = symbolicName;
            Properties = properties;
            Labels = labels;
        }

        public override ClauseKind Kind => ClauseKind.Node;

        public SymbolicName? SymbolicName { get; }

        public SymbolicName RequiredSymbolicName { get; }

        public Properties? Properties { get; }

        public List<NodeLabel>? Labels { get; }

        /// <summary>
        /// Creates a copy of this node with a new symbolic name.
        /// </summary>
        /// <param name="newSymbolicName">The new symbolic name.</param>
        /// <returns>The new node</returns>
        public Node Named(string newSymbolicName)
        {
            Ensure.HasText(newSymbolicName, "Symbolic name is required");
            return new Node(SymbolicName.Of(newSymbolicName), Properties, Labels);
        }

        /// <summary>
        /// Creates a copy of this node with a new symbolic name.
        /// </summary>
        /// <param name="newSymbolicName">The new symbolic name.</param>
        /// <returns>The new node</returns>
        public Node Named(SymbolicName newSymbolicName)
        {
            Ensure.IsNotNull(newSymbolicName, "Symbolic name is required");
            return new Node(newSymbolicName, Properties, Labels);
        }

        public Node WithProperties(MapExpression? newProperties) =>
            new(SymbolicName,
                newProperties == null ? null : new Properties(newProperties),
                Labels);

        public Node WithProperties(params object[] keysAndValues)
        {
            MapExpression? newProperties = null;
            if (keysAndValues.Length != 0)
            {
                newProperties = MapExpression.Create(keysAndValues);
            }

            return WithProperties(newProperties);
        }

        public Property Property(string name) => Property(new[]
        {
            name
        });

        public Property Property(params string[] names) =>
            Language.Property.Create(this, names);

        public Property Property(Expression lookup) =>
            Language.Property.Create(this, lookup);

        public MapProjection Project(List<object> entries) =>
            Project(entries.ToArray());

        public MapProjection Project(params object[] entries) =>
            RequiredSymbolicName.Project(entries);

        public static Node Create(string primaryLabel)
        {
            return new(primaryLabel, null, null);
        }

        public static Node Create()
        {
            return new(null, null);
        }

        public static Node Create(string primaryLabel, string[] additionalLabels)
        {
            return Create(primaryLabel, null, additionalLabels);
        }

        public static Node Create(
            string primaryLabel,
            MapExpression? properties,
            string[]? additionalLabels)
        {
            return new(
                primaryLabel,
                properties != null ? new Properties(properties) : null,
                additionalLabels);
        }

        public Relationship RelationshipTo(Node other, params string[] types) =>
            Relationship.Create(this, RelationshipDirection.Outgoing, other, types);

        public Relationship RelationshipFrom(Node other, params string[] types) =>
            Relationship.Create(this, RelationshipDirection.Incoming, other, types);

        public Relationship RelationshipBetween(Node other, params string[] types) =>
            Relationship.Create(this, RelationshipDirection.None, other, types);

        public Condition? IsEqualTo(Node otherNode) =>
            RequiredSymbolicName.IsEqualTo(otherNode.RequiredSymbolicName);

        public Condition? IsNotEqualTo(Node otherNode) =>
            RequiredSymbolicName.IsNotEqualTo(otherNode.RequiredSymbolicName);

        public Condition? IsNull() => RequiredSymbolicName?.IsNull();

        public Condition? IsNotNull() => RequiredSymbolicName?.IsNotNull();

        public SortItem? Descending => RequiredSymbolicName?.Descending();

        public SortItem? Ascending => RequiredSymbolicName?.Ascending();

        public AliasedExpression? As(string alias) =>
            RequiredSymbolicName.As(alias);

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            SymbolicName?.Visit(cypherVisitor);
            Labels?.ForEach(label => label.Visit(cypherVisitor));
            Properties?.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
