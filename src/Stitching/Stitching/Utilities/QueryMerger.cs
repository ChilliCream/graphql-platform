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
        : QuerySyntaxRewriter<QueryMergeRewriter.QueryMergeRewriterContext>
    {

        protected override FragmentSpreadNode RewriteFragmentSpread(
            FragmentSpreadNode node,
            QueryMergeRewriterContext context)
        {
            return node.WithName(context.CreateNewName(node.Name));
        }

        protected override FragmentDefinitionNode RewriteFragmentDefinition(
            FragmentDefinitionNode node,
            QueryMergeRewriterContext context)
        {
            return base.RewriteFragmentDefinition(
                node.WithName(context.CreateNewName(node.Name)),
                context);
        }


        public class QueryMergeRewriterContext
        {
            public string RequestName;

            public NameNode CreateNewName(NameNode name)
            {
                return new NameNode($"{RequestName}_{name.Value}");
            }
        }
    }


}
