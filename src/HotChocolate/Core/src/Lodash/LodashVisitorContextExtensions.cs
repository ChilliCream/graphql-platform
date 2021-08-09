using System;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Lodash
{
    public static class LodashVisitorContextExtensions
    {
        public static LodashJsonRewriter CreateRewriter(this LodashVisitorContext context)
        {
            return new LodashJsonRewriter(
                new LodashRewriterStep(
                    string.Empty,
                    context.Operations.Peek().Count == 0
                        ? Array.Empty<LodashOperation>()
                        : context.Operations.Peek().ToArray(),
                    context.Steps.Peek().ToArray()));
        }
    }
}
