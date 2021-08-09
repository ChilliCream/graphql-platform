using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Lodash
{
    public class LodashSyntaxVisitor : SyntaxWalker<LodashVisitorContext>
    {
        private readonly ILodashOperationFactory[] _factories =
        {
            LodashMapOperation.Factory,
            LodashCountByOperation.Factory,
            LodashGroupByOperation.Factory,
            LodashKeyByOperation.Factory,
            LodashChunkOperation.Factory,
            LodashTakeOperation.Factory,
            LodashTakeRightOperation.Factory,
            LodashDropOperation.Factory,
            LodashDropRightOperation.Factory
        };

        private IReadOnlyList<LodashOperation> CreateOperations(
            IReadOnlyList<DirectiveNode> directives)
        {
            List<LodashOperation>? operations = null;
            foreach (var directive in directives)
            {
                if (directive.Name.Value == "_")
                {
                    foreach (var factory in _factories)
                    {
                        if (factory.TryCreateOperation(directive, out LodashOperation? operation))
                        {
                            operations ??= new List<LodashOperation>();
                            operations.Add(operation);
                        }
                    }
                }
            }

            return operations?.ToArray() ?? Array.Empty<LodashOperation>();
        }

        protected override ISyntaxVisitorAction Enter(FieldNode node, LodashVisitorContext context)
        {
            context.Steps.Push(new List<LodashRewriterStep>());
            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(FieldNode node, LodashVisitorContext context)
        {
            IReadOnlyList<LodashOperation> operations = CreateOperations(node.Directives);
            IList<LodashRewriterStep> nextSteps = context.Steps.Pop();
            if (operations.Count > 0 || nextSteps.Count > 0)
            {
                LodashRewriterStep[] steps = nextSteps.Count > 0
                    ? nextSteps.ToArray()
                    : Array.Empty<LodashRewriterStep>();

                context.Steps.Peek().Push(new LodashRewriterStep(GetName(node), operations, steps));
            }

            return Continue;
        }

        private static string GetName(FieldNode node)
        {
            return node.Alias?.Value ?? node.Name.Value;
        }
    }
}
