using System;
using System.Linq;
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

            if (!context.StartNodes.Any())
            {
                context.Projections.Add(field.GetName());
                return true;
            }
            if(context.StartNodes.Count != context.EndNodes.Count)
                context.EndNodes.Push(Cypher.NamedNode(selection.DeclaringType.Name.Value));

            if (context.StartNodes.Count != context.Relationships.Count)
            {
                Neo4JRelationshipAttribute rel = context.RelationshipTypes.Peek();
                Node startNode = context.StartNodes.Peek();
                Node endNode = context.EndNodes.Peek();
                switch (rel.Direction)
                {
                    case RelationshipDirection.Incoming:
                        context.Relationships.Push(startNode.RelationshipFrom(endNode, rel.Name));
                        break;
                    case RelationshipDirection.Outgoing:
                        context.Relationships.Push(startNode.RelationshipTo(endNode, rel.Name));
                        break;
                    case RelationshipDirection.None:
                        context.Relationships.Push(startNode.RelationshipBetween(endNode, rel.Name));
                        break;
                    default:
                        throw new InvalidOperationException("Relationship direction not set!");
                        break;
                }
            }
            context.RelationshipProjections[context.CurrentLevel].Enqueue(selection.Field.GetName());

            return true;
        }
    }
}
