using System.Collections.Generic;
using HotChocolate.Resolvers;

namespace HotChocolate.Stitching
{
    internal interface IScopedVariableResolver
    {
        VariableValue Resolve(
            IMiddlewareContext context,
            IReadOnlyDictionary<string, object> variables,
            ScopedVariableNode variable);
    }
}
