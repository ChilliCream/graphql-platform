using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching
{
    public class QueryMerger
    {
        private readonly List<DocumentNode> _queryies =
            new List<DocumentNode>();


        public QueryMerger AddQuery(DocumentNode query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            _queryies.Add(query);
            return this;
        }

        public DocumentNode Build()
        {
            return null;
        }


    }

    public class QueryMergeRewriter
        : QuerySyntaxRewriter<bool>
    {
        private readonly List<FieldNode> _fields = new List<FieldNode>();
        private Dictionary<string, FragmentDefinitionNode> _fragments =
            new Dictionary<string, FragmentDefinitionNode>();

        private string _requestName;
        private bool _rewriteFragments;




        protected override FieldNode RewriteField(FieldNode node, bool first)
        {
            FieldNode current = node;
            NameNode alias = CreateNewName(node.Alias ?? node.Name);

            current = current.WithAlias(alias);

            current = Rewrite(current, node.Arguments, first,
                (p, c) => RewriteMany(p, c, RewriteArgument),
                current.WithArguments);

            if (node.SelectionSet != null)
            {
                current = Rewrite(current, node.SelectionSet, false,
                    RewriteSelectionSet, current.WithSelectionSet);
            }

            return current;
        }

        protected override FragmentSpreadNode RewriteFragmentSpread(
            FragmentSpreadNode node, bool first)
        {
            return _rewriteFragments
                ? node.WithName(CreateNewName(node.Name))
                : node;
        }

        protected override FragmentDefinitionNode RewriteFragmentDefinition(
            FragmentDefinitionNode node, bool first)
        {
            return _rewriteFragments
                ? base.RewriteFragmentDefinition(
                    node.WithName(CreateNewName(node.Name)),
                    first)
                : base.RewriteFragmentDefinition(node, first);
        }

        public NameNode CreateNewName(NameNode name)
        {
            return new NameNode($"{_requestName}_{name.Value}");
        }
    }
}
