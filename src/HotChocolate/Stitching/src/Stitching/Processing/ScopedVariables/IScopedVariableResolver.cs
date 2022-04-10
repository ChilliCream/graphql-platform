using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Processing.ScopedVariables;

internal interface IScopedVariableResolver
{
    ScopedVariableValue Resolve(
        IResolverContext context,
        ScopedVariableNode variable,
        IInputType targetType);
}
