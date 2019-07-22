using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Execution.Batching
{
    internal class CollectVariablesVisitor
        : ISyntaxNodeVisitor<OperationDefinitionNode>
        , ISyntaxNodeVisitor<VariableNode>
    {
        private readonly HashSet<string> _names = new HashSet<string>();
        private bool _first;
        private string _name;

        public IReadOnlyList<string> GetVariableNames() => _names.ToArray();

        public void Prepare(string operationName)
        {
            _name = operationName;
            _names.Clear();
        }

        public VisitorAction Enter(
            OperationDefinitionNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            if (_name == null)
            {
                return _first
                    ? VisitorAction.Skip
                    : VisitorAction.Continue;
            }

            return _name.Equals(
                node.Name?.Value,
                StringComparison.Ordinal)
                ? VisitorAction.Continue
                : VisitorAction.Skip;
        }

        public VisitorAction Leave(
            OperationDefinitionNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            for (int i = 0; i < node.VariableDefinitions.Count; i++)
            {
                _names.Remove(
                    node.VariableDefinitions[i].Variable.Name.Value);
            }
            return VisitorAction.Continue;
        }

        public VisitorAction Enter(
            VariableNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            _names.Add(node.Value);
            return VisitorAction.Skip;
        }

        public VisitorAction Leave(
            VariableNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Skip;
        }
    }
}
