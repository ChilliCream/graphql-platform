using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Lodash
{
    public class AggregationSyntaxVisitor : SyntaxWalker<AggregationVisitorContext>
    {
        private IReadOnlyList<AggregationOperation> CreateOperations(
            AggregationVisitorContext context,
            IReadOnlyList<DirectiveNode> directives)
        {
            List<AggregationOperation>? operations = null;
            foreach (var directive in directives)
            {
                if (context.Directives.TryGetValue(
                    directive.Name.Value,
                    out IAggregationDirectiveType? directiveType))
                {
                    operations ??= new List<AggregationOperation>();
                    operations.Add(directiveType.CreateOperation(directive));
                }
            }

            return operations?.ToArray() ?? Array.Empty<AggregationOperation>();
        }

        protected override ISyntaxVisitorAction Enter(FieldNode node, AggregationVisitorContext context)
        {
            context.Steps.Push(new List<AggregationRewriterStep>());
            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(FieldNode node, AggregationVisitorContext context)
        {
            IReadOnlyList<AggregationOperation> operations =
                CreateOperations(context, node.Directives);

            IList<AggregationRewriterStep> nextSteps = context.Steps.Pop();
            if (operations.Count > 0 || nextSteps.Count > 0)
            {
                AggregationRewriterStep[] steps = nextSteps.Count > 0
                    ? nextSteps.ToArray()
                    : Array.Empty<AggregationRewriterStep>();

                context.Steps.Peek()
                    .Push(new AggregationRewriterStep(GetName(node), operations, steps));
            }

            return Continue;
        }

        private static string GetName(FieldNode node)
        {
            return node.Alias?.Value ?? node.Name.Value;
        }
    }
}
