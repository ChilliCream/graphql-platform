using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.Neo4J.Language;
using HotChocolate.Data.Projections;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Data.Neo4J.Projections
{
    /// <inheritdoc/>
    public class Neo4JProjectionFieldHandler
        : Neo4JProjectionHandlerBase
    {
        /// <inheritdoc/>
        public override bool CanHandle(ISelection selection) =>
            selection.SelectionSet is not null;

        /// <inheritdoc/>
        public override bool TryHandleEnter(
            Neo4JProjectionVisitorContext context,
            ISelection selection,
            [NotNullWhen(true)] out ISelectionVisitorAction? action)
        {
            context.IsRelationship = true;
            context.ParentNode ??= Cypher.NamedNode(selection.DeclaringType.Name.Value);
            context.Projections.Push(selection.Field.GetName());
            action = SelectionVisitor.Continue;
            return true;
        }

        /// <inheritdoc/>
        public override bool TryHandleLeave(
            Neo4JProjectionVisitorContext context,
            ISelection selection,
            [NotNullWhen(true)] out ISelectionVisitorAction? action)
        {
            context.TryCreateRelationshipProjection(out PatternComprehension? projections);
            context.Projections.Push(projections);

            context.IsRelationship = false;
            context.Relationship = null;
            context.CurrentNode = null;
            context.ParentNode = null;
            context.RelationshipProjections.Clear();
            action = SelectionVisitor.Continue;
            return true;
        }
    }
}
