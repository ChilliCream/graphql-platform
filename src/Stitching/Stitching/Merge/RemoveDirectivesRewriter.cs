using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Stitching.Merge
{
    internal sealed class RemoveDirectivesRewriter
        : SchemaSyntaxRewriter<object>
    {
        private readonly HashSet<NameString> _knownDirectives =
            new HashSet<NameString>
            {
                DirectiveNames.Computed,
                DirectiveNames.Delegate,
                DirectiveNames.Source
            };

        public DocumentNode RemoveDirectives(
            DocumentNode document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return RewriteDocument(document, new object());
        }

        protected override TParent RewriteDirectives<TParent>(
            TParent parent,
            IReadOnlyList<DirectiveNode> directives,
            object context,
            Func<IReadOnlyList<DirectiveNode>, TParent> rewrite)
        {
            var rewritten = new List<DirectiveNode>();

            foreach (DirectiveNode directive in directives)
            {
                if (_knownDirectives.Contains(directive.Name.Value))
                {
                    rewritten.Add(directive);
                }
            }

            return rewrite(rewritten);
        }
    }
}
