using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Delegation
{
    internal interface IScopedVariableResolver
    {
        VariableValue Resolve(
            IResolverContext context,
            ScopedVariableNode variable,
            IInputType targetType);
    }
}
