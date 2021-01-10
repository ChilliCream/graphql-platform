using System.Collections.Generic;
using HotChocolate.Data.Neo4J.Utils;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// https://s3.amazonaws.com/artifacts.opencypher.org/railroad/NodePattern.html
    /// </summary>
    public class Node :
        PatternElement,
        IPropertyContainer,
        //IExposesRelationship<Relationship>,
        IExposesProperties<Node>
    {
        public override ClauseKind Kind => ClauseKind.Node;
        private readonly SymbolicName? _symbolicName;
        private readonly List<NodeLabel> _labels;
        private readonly Properties? _properties;
        public SymbolicName? GetSymbolicName() => _symbolicName;

        private Node(string primaryLabel, Properties? properties, string[]? additionalLabels)
        {
            _symbolicName = null;
            _labels = new List<NodeLabel>();

            if (!string.IsNullOrEmpty(primaryLabel))
            {
                _labels.Add(new NodeLabel(primaryLabel));
            }

            if (!(additionalLabels == null || additionalLabels.Length == 0))
            {
                foreach (var nodeLabel in additionalLabels)
                {
                    _labels.Add(new NodeLabel(nodeLabel));
                }
            }

            _properties = properties;
        }

        private Node(SymbolicName? symbolicName, Properties properties, List<NodeLabel> labels)
        {
            _symbolicName = symbolicName;
            _properties = properties;
            _labels = labels;
        }

        /// <summary>
        /// Creates a copy of this node with a new symbolic name.
        /// </summary>
        /// <param name="newSymbolicName"></param>
        /// <returns></returns>
        public Node Named(string newSymbolicName)
        {
            Assertions.HasText(newSymbolicName, "Symbolic name is required");
            return new Node(SymbolicName.Of(newSymbolicName), _properties, _labels);
        }

        /// <summary>
        /// Creates a copy of this node with a new symbolic name.
        /// </summary>
        /// <param name="newSymbolicName"></param>
        /// <returns></returns>
        public Node Named(SymbolicName newSymbolicName) => new Node(newSymbolicName, _properties, _labels);

        public Node WithProperties(MapExpression? newProperties) => new(_symbolicName, newProperties == null ? null : new Properties(newProperties), _labels);

        public Node WithProperties(params object[] keysAndValues)
        {
            MapExpression? newProperties = null;
            if(keysAndValues.Length != 0)
            {
                newProperties = MapExpression.Create(keysAndValues);
            }
            return WithProperties(newProperties);
        }

        public Property Property(string name)
        {
            return Language.Property.Create(this, name);
        }

        public static Node Create(string primaryLabel)
        {
            return new (primaryLabel, null, null);
        }

        public static Node Create(string primaryLabel, string[] additionalLabels)
        {
            return Create(primaryLabel, null, additionalLabels);
        }

        public static Node Create(string primaryLabel, MapExpression properties, string[]? aditionalLabels)
        {
            return new (primaryLabel, properties != null ? new Properties(properties) : null, aditionalLabels);
        }

        public Relationship RelationshipTo(Node other, params string[] types) => new Relationship();
        public Relationship RelationshipFrom(Node other, params string[] types) => new Relationship();
        public Relationship RelationshipBetween(Node other, params string[] types) => new Relationship();

        public new void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            _symbolicName?.Visit(visitor);
            _labels.ForEach(label => label.Visit(visitor));
            _properties?.Visit(visitor);
            visitor.Leave(this);
        }
    }
}
