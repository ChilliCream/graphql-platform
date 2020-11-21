using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// https://s3.amazonaws.com/artifacts.opencypher.org/railroad/NodePattern.html
    /// </summary>
    public class Node : PatternElement, PropertyContainer, ExposeRelationship<Relationship>, ExposesProperties<Node>
    {
        private readonly SymbolicName _symbolicName;
        private readonly List<NodeLabel> _labels;
        private readonly Properties _properties;

        private Node(string primaryLabel, Properties properties, string[] additionalLabels)
        {
            _symbolicName = null;
            _labels = new List<NodeLabel>();

            if (!string.IsNullOrEmpty(primaryLabel))
            {
                _labels.Add(new NodeLabel(primaryLabel));
            }

            if (!(additionalLabels?.Length != 0))
            {
                foreach (string nodeLabel in additionalLabels)
                {
                    _labels.Add(new NodeLabel(nodeLabel));
                }
            }
        }

        private Node(SymbolicName symbolicName, Properties properties, List<NodeLabel> labels)
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
        public Node Named(string newSymbolicName) => new Node(SymbolicName.Of(newSymbolicName), _properties, _labels);

        /// <summary>
        /// Creates a copy of this node with a new symbolic name.
        /// </summary>
        /// <param name="newSymbolicName"></param>
        /// <returns></returns>
        public Node Named(SymbolicName newSymbolicName) => new Node(newSymbolicName, _properties, _labels);

        public Node WithProperties(MapExpression newProperties) => new Node(_symbolicName, newProperties == null ? null : new Properties(newProperties), _labels);

    }
}