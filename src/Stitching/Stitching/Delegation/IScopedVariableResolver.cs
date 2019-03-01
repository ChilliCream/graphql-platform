using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Stitching.Delegation
{
    internal interface IScopedVariableResolver
    {
        VariableValue Resolve(
            IResolverContext context,
            ScopedVariableNode variable,
            ITypeNode targetType);
    }
}
