using System;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Lodash
{
    public static class AggregationVisitorContextExtensions
    {
        public static AggregationJsonRewriter CreateRewriter(this AggregationVisitorContext context)
        {
            return new AggregationJsonRewriter(
                new AggregationRewriterStep(
                    string.Empty,
                    context.Operations.Peek().Count == 0
                        ? Array.Empty<AggregationOperation>()
                        : context.Operations.Peek().ToArray(),
                    context.Steps.Peek().ToArray()));
        }
    }
}
