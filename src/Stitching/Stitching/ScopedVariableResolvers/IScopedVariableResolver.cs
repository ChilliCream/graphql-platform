using System.Collections.Generic;
using HotChocolate.Resolvers;

namespace HotChocolate.Stitching
{
    internal interface IScopedVariableResolver
    {
        VariableValue Resolve(
            IResolverContext context,
            ScopedVariableNode variable);
    }
}
