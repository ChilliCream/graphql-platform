using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// https://s3.amazonaws.com/artifacts.opencypher.org/railroad/NodePattern.html
    /// </summary>
    public class Node :
        Visitable,
        IPatternElement,
        IPropertyContainer,
        IExposesRelationships<Relationship>,
        IExposesProperties<Node>
    {
        public override ClauseKind Kind => ClauseKind.Node;
        private readonly SymbolicName? _symbolicName;
        private readonly List<NodeLabel>? _labels;
        private readonly Properties? _properties;
        public SymbolicName? GetSymbolicName() => _symbolicName;
        public SymbolicName GetRequiredSymbolicName() => _symbolicName;

        private Node(string? primaryLabel, Properties? properties, params string[]? additionalLabels)
        {
            _symbolicName = null;
            _labels = new List<NodeLabel>();

            if (!string.IsNullOrEmpty(primaryLabel))
            {
                _labels.Add(new NodeLabel(primaryLabel));
            }

            if (additionalLabels != null && additionalLabels.Any())
            {
                foreach (var nodeLabel in additionalLabels)
                {
                    _labels.Add(new NodeLabel(nodeLabel));
                }
            }

            _properties = properties;
        }

        private Node(SymbolicName? symbolicName, Properties? properties, List<NodeLabel>? labels)
        {
            _symbolicName = symbolicName;
            _properties = properties;
            _labels = labels;
        }

        /// <summary>
        /// Creates a copy of this node with a new symbolic name.
        /// </summary>
        /// <param name="newSymbolicName">The new symbolic name.</param>
        /// <returns>The new node</returns>
        public Node Named(string newSymbolicName)
        {
            Ensure.HasText(newSymbolicName, "Symbolic name is required");
            return new Node(SymbolicName.Of(newSymbolicName), _properties, _labels);
        }

        /// <summary>
        /// Creates a copy of this node with a new symbolic name.
        /// </summary>
        /// <param name="newSymbolicName">The new symbolic name.</param>
        /// <returns>The new node</returns>
        public Node Named(SymbolicName newSymbolicName)
        {
            Ensure.IsNotNull(newSymbolicName, "Symbolic name is required");
            return new Node(newSymbolicName, _properties, _labels);
        }

        public Node WithProperties(MapExpression? newProperties) =>
            new(_symbolicName, newProperties == null ? null : new Properties(newProperties), _labels);

        public Node WithProperties(params object[] keysAndValues)
        {
            MapExpression? newProperties = null;
            if(keysAndValues.Length != 0)
            {
                newProperties = MapExpression.Create(keysAndValues);
            }
            return WithProperties(newProperties);
        }

        public Property Property(string name) => Property(new[] { name });

        public Property Property(params string[] names) =>
            Language.Property.Create(this, names);

        public Property Property(Expression lookup) =>
            Language.Property.Create(this,lookup);

        public MapProjection Project(List<object> entries) =>
            Project(entries.ToArray());

        public MapProjection Project(params object[] entries) =>
            GetRequiredSymbolicName().Project(entries);

        public static Node Create(string primaryLabel)
        {
            return new (primaryLabel, null, null);
        }

        public static Node Create()
        {
            return new (null, null);
        }

        public static Node Create(string primaryLabel, string[] additionalLabels)
        {
            return Create(primaryLabel, null, additionalLabels);
        }

        public static Node Create(string primaryLabel, MapExpression? properties, string[]? additionalLabels)
        {
            return new (primaryLabel, properties != null ? new Properties(properties) : null, additionalLabels);
        }

        public Relationship RelationshipTo(Node other, params string[] types) =>
            Relationship.Create(this, RelationshipDirection.Outgoing, other, types);

        public Relationship RelationshipFrom(Node other, params string[] types) =>
            Relationship.Create(this, RelationshipDirection.Incoming, other, types);
        public Relationship RelationshipBetween(Node other, params string[] types) =>
            Relationship.Create(this, RelationshipDirection.None, other, types);

        public Condition? IsEqualTo(Node otherNode) =>
            GetRequiredSymbolicName()?.IsEqualTo(otherNode.GetRequiredSymbolicName());

        public Condition? IsNotEqualTo(Node otherNode) =>
            GetRequiredSymbolicName()?.IsNotEqualTo(otherNode.GetRequiredSymbolicName());

        public Condition? IsNull()  => GetRequiredSymbolicName()?.IsNull();

        public Condition? IsNotNull()  => GetRequiredSymbolicName()?.IsNotNull();

        public SortItem? Descending => GetRequiredSymbolicName()?.Descending();

        public SortItem? Ascending => GetRequiredSymbolicName()?.Ascending();

        public AliasedExpression? As(string alias) =>
            GetRequiredSymbolicName().As(alias);

        public IReadOnlyList<NodeLabel>? GetLabels() => _labels?.AsReadOnly();

        public Condition HasLabels(params string[] labelsToQuery) =>
            HasLabelCondition.Create(_symbolicName)
            ?? throw new InvalidOperationException("Cannot query a node without a symbolic name.");

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _symbolicName?.Visit(cypherVisitor);
            _labels?.ForEach(label => label.Visit(cypherVisitor));
            _properties?.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
