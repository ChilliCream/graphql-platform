using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching
{
    internal class ExtractRemoteQueryRewriter
        : QuerySyntaxRewriter<string>
    {
        protected override FieldNode RewriteField(
            FieldNode node,
            string schemaName)
        {
            FieldNode current = node;

            current = current.WithDirectives(
                RemoveStitchingDirectives(current.Directives));

            return base.RewriteField(current, schemaName);
        }

        protected override SelectionSetNode RewriteSelectionSet(
            SelectionSetNode node,
            string schemaName)
        {
            var selections = new List<ISelectionNode>();

            foreach (ISelectionNode selection in node.Selections)
            {
                if ((IsRelevant(selection, schemaName)))
                {
                    selections.Add(selection);
                }
            }

            return base.RewriteSelectionSet(
                node.WithSelections(selections),
                schemaName);
        }

        private bool IsRelevant(ISelectionNode selection, string schemaName)
        {
            if (selection.Directives.Any(t => t.IsDelegateDirective()))
            {
                return false;
            }

            return selection.Directives
                .Any(t => t.IsSchemaDirective(schemaName));
        }

        private IReadOnlyCollection<DirectiveNode> RemoveStitchingDirectives(
            IEnumerable<DirectiveNode> directives)
        {
            return directives.Where(t => !t.IsStitchingDirective()).ToList();
        }
    }
}
