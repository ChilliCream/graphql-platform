using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Execution.Batching
{
    public sealed class CollectVariablesVisitationMap
        : VisitationMap
    {
        private IReadOnlyDictionary<string, FragmentDefinitionNode>? _fragments = null;

        public void Initialize(IReadOnlyDictionary<string, FragmentDefinitionNode> fragments)
        {
            _fragments = fragments ?? 
                throw new ArgumentNullException(nameof(fragments));
        }

        protected override void ResolveChildren(
           OperationDefinitionNode node,
           IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.SelectionSet),
                node.SelectionSet,
                children);
        }

        protected override void ResolveChildren(
            VariableNode node,
            IList<SyntaxNodeInfo> children)
        {
            // we do not want to visit any nodes.
        }

        protected override void ResolveChildren(
            FieldNode node,
            IList<SyntaxNodeInfo> children)
        {
            if (node.Arguments.Count != 0)
            {
                ResolveChildren(
                    nameof(node.Arguments),
                    node.Arguments,
                    children);
            }

            if (node.SelectionSet != null)
            {
                ResolveChildren(
                    nameof(node.SelectionSet),
                    node.SelectionSet,
                    children);
            }
        }

        protected override void ResolveChildren(
            ArgumentNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Value),
                node.Value,
                children);
        }

        protected override void ResolveChildren(
            FragmentSpreadNode node,
            IList<SyntaxNodeInfo> children)
        {
            if (_fragments is not null && 
                _fragments.TryGetValue(node.Name.Value, out FragmentDefinitionNode? d))
            {
                ResolveChildren(
                    d.Name.Value,
                    d,
                    children);
            }
        }

        protected override void ResolveChildren(
            InlineFragmentNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.SelectionSet),
                node.SelectionSet,
                children);
        }

        protected override void ResolveChildren(
            FragmentDefinitionNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.SelectionSet),
                node.SelectionSet,
                children);
        }
    }
}
