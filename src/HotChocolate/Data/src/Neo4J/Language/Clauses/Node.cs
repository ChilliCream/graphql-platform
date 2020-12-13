using System.Collections.Generic;
using HotChocolate.Data.Neo4J.Utils;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// https://s3.amazonaws.com/artifacts.opencypher.org/railroad/NodePattern.html
    /// </summary>
    public class Node : Visitable
    {
        public new ClauseKind Kind { get; } = ClauseKind.Node;
        private readonly SymbolicName? _symbolicName;
        private readonly List<NodeLabel> _labels;
        private readonly Properties _properties;

        private Node(string primaryLabel, Properties? properties, string[]? additionalLabels)
        {
            _symbolicName = null;
            _labels = new List<NodeLabel>();

            if (!string.IsNullOrEmpty(primaryLabel))
            {
                _labels.Add(new NodeLabel(primaryLabel));
            }

            if (!(additionalLabels?.Length != 0))
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

        public Node WithProperties(MapExpression newProperties) => new Node(_symbolicName, newProperties == null ? null : new Properties(newProperties), _labels);

        public Node WithProperties(object[] keyAndValues)
        {
            MapExpression newProperties = null;
            if(keyAndValues != null && keyAndValues.Length != 0)
            {
                newProperties = MapExpression.Create(keyAndValues);
            }
            return WithProperties(newProperties);
        }

        //public Property property(string name)
        //{
        //    return Property.Create(this, name);
        //}

        public static Node Create(string primaryLabel)
        {
            return new Node(primaryLabel, null, null);
        }

        public static Node Create(string primaryLabel, string[] additionalLabels)
        {
            return Create(primaryLabel, null, additionalLabels);
        }

        public static Node Create(string primaryLabel, MapExpression? properties, string[]? aditionalLabels)
        {
            return new Node(primaryLabel, properties != null ? new Properties(properties) : null, aditionalLabels);
        }

        public new void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            VisitIfNotNull(_symbolicName, visitor);
            _labels.ForEach(label => label.Visit(visitor));
            VisitIfNotNull(_properties, visitor);
            visitor.Leave(this);
        }
    }
}
