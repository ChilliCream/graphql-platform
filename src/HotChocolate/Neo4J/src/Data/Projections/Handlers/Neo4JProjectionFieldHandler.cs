using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
            ++context.CurrentLevel;
            selection.Field.ContextData.TryGetValue(nameof(Neo4JRelationshipAttribute), out object? relationship);
            if (relationship is Neo4JRelationshipAttribute rel)
            {
                context.RelationshipTypes.Push(rel);
            }

            context.StartNodes.Push(Cypher.NamedNode(selection.DeclaringType.Name.Value));

            if (context.RelationshipProjections.ContainsKey(context.CurrentLevel))
            {
                context.RelationshipProjections[context.CurrentLevel].Enqueue(selection.Field.GetName());
            }
            else
            {
                Queue<object> queue = new ();
                queue.Enqueue(selection.Field.GetName());
                context.RelationshipProjections.Add(context.CurrentLevel, queue);
            }

            action = SelectionVisitor.Continue;
            return true;
        }

        /// <inheritdoc/>
        public override bool TryHandleLeave(
            Neo4JProjectionVisitorContext context,
            ISelection selection,
            [NotNullWhen(true)] out ISelectionVisitorAction? action)
        {
            if (context.StartNodes.Any())
            {
                object? field = context.RelationshipProjections[context.CurrentLevel].Dequeue();

                context.TryCreateRelationshipProjection(out PatternComprehension? projections);

                switch (context.CurrentLevel)
                {
                    case > 1:
                        context.RelationshipProjections[context.CurrentLevel - 1].Enqueue(field);
                        context.RelationshipProjections[context.CurrentLevel - 1].Enqueue(projections);
                        break;
                    case 1:
                        context.Projections.Push(field);
                        context.Projections.Push(projections);
                        break;
                }
            }

            --context.CurrentLevel;

            context.StartNodes.Pop();
            context.EndNodes.Pop();
            context.Relationships.Pop();

            if (context.CurrentLevel == 0)
            {
                context.RelationshipProjections.Clear();
            }

            action = SelectionVisitor.Continue;
            return true;
        }
    }
}
