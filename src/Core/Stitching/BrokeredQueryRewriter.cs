using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching
{
    public class BrokeredQueryRewriter
        : QuerySyntaxRewriter<string>
    {
        protected override FieldNode VisitField(
            FieldNode node,
            string schemaName)
        {
            FieldNode current = node;

            current = current.WithDirectives(
                RemoveStitchingDirectives(current.Directives));

            return base.VisitField(current, schemaName);
        }

        protected override SelectionSetNode VisitSelectionSet(
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

            return base.VisitSelectionSet(
                node.WithSelections(selections),
                schemaName);
        }

        private bool IsRelevant(ISelectionNode selection, string schemaName)
        {
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
