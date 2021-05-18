using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace HotChocolate.Execution.Processing.Plan
{
    /// <summary>
    /// Represents a executable part of the query tree.
    /// </summary>
    internal abstract class QueryPlanNode
    {
        private readonly List<QueryPlanNode> _nodes = new();

        protected QueryPlanNode(ExecutionStrategy strategy)
        {
            Strategy = strategy;
        }

        /// <summary>
        /// Gets the strategy with which this node can be executed.
        /// </summary>
        public ExecutionStrategy Strategy { get; }

        /// <summary>
        /// Gets the parent node.
        /// </summary>
        public QueryPlanNode? Parent { get; private set; }

        /// <summary>
        /// Gets the child nodes.
        /// </summary>
        public IReadOnlyList<QueryPlanNode> Nodes => _nodes;

        /// <summary>
        /// Adds a child node.
        /// </summary>
        /// <param name="node">The child node.</param>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="node"/> is <c>null</c>.
        /// </exception>
        public void AddNode(QueryPlanNode node)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            node.Parent = this;
            _nodes.Add(node);
        }

        /// <summary>
        /// Removes the specified child <paramref name="node"/>.
        /// </summary>
        /// <param name="node">
        /// The child node that shall be removed.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="node"/> is <c>null</c>.
        /// </exception>
        public void RemoveNode(QueryPlanNode node)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            node.Parent = null;
            _nodes.Remove(node);
        }

        /// <summary>
        /// Tries to take the first child node.
        /// If this node has child node the first child node is removed and
        /// returned through the output parameter <paramref name="node"/>.
        /// </summary>
        /// <param name="node">The removed child node.</param>
        /// <returns>
        /// <c>true</c> if a child node could be removed; otherwise, <c>false</c>.
        /// </returns>
        public bool TryTakeNode([MaybeNullWhen(false)] out QueryPlanNode node)
        {
            if (_nodes.Count > 0)
            {
                node = _nodes[0];
                node.Parent = null;
                _nodes.RemoveAt(0);
                return true;
            }

            node = null;
            return false;
        }

        /// <summary>
        /// Creates an executable query step that for the query plan.
        /// </summary>
        public abstract QueryPlanStep CreateStep();

        /// <summary>
        /// Serializes the current node to JSON.
        /// </summary>
        /// <param name="writer">
        /// The JSON writer that is used for serializing this node.
        /// </param>
        public abstract void Serialize(Utf8JsonWriter writer);

        /// <summary>
        /// Serializes the current node to a GraphQL extension response structure.
        /// </summary>
        public abstract object Serialize();
    }
}
