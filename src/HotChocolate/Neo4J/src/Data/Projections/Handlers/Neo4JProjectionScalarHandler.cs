using HotChocolate.Data.Neo4J.Language;
using HotChocolate.Data.Projections;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;

namespace HotChocolate.Data.Neo4J.Projections
{
    /// <inheritdoc/>
    public class Neo4JProjectionScalarHandler
        : Neo4JProjectionHandlerBase
    {
        /// <inheritdoc/>
        public override bool CanHandle(ISelection selection) =>
            selection.SelectionSet is null;

        /// <inheritdoc/>
        public override bool TryHandleEnter(
            Neo4JProjectionVisitorContext context,
            ISelection selection,
            out ISelectionVisitorAction action)
        {
            IObjectField field = selection.Field;
            action = SelectionVisitor.SkipAndLeave;

            if (context.IsRelationship == false)
            {
                context.Projections.Add(field.GetName());
                return true;
            }


            context.CurrentNode ??= Cypher.NamedNode(selection.DeclaringType.Name.Value);
            context.Relationship ??= context.ParentNode?.RelationshipTo(context.CurrentNode, "RELATED_TO");

            context.RelationshipProjections.Add(field.GetName());

            return true;
        }
    }
}
