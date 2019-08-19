using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace StrawberryShake.Generators
{
    public sealed class DocumentVisitationMap
        : VisitationMap
    {
        private IReadOnlyDictionary<string, FragmentDefinitionNode> _fragments;

        public void Initialize(
            IReadOnlyDictionary<string, FragmentDefinitionNode> fragments)
        {
            _fragments = fragments
                ?? throw new ArgumentNullException(nameof(fragments));
        }

        protected override void ResolveChildren(
            DocumentNode node,
            IList<SyntaxNodeInfo> children)
        {
            int i = 0;
            foreach (IDefinitionNode definition in node.Definitions)
            {
                if (definition is OperationDefinitionNode)
                {
                    children.Add(new SyntaxNodeInfo(
                        definition, nameof(node.Definitions), i++));
                }
            }
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
            if (node.SelectionSet != null)
            {
                ResolveChildren(
                    nameof(node.SelectionSet),
                    node.SelectionSet,
                    children);
            }
        }

        protected override void ResolveChildren(
            FragmentSpreadNode node,
            IList<SyntaxNodeInfo> children)
        {
            if (_fragments.TryGetValue(
                node.Name.Value,
                out FragmentDefinitionNode d))
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
