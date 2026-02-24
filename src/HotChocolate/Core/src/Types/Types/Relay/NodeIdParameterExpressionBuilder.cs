using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Resolvers.Expressions.Parameters;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Relay;

internal sealed class NodeIdParameterExpressionBuilder
    : ScopedStateParameterExpressionBuilder
{
    public override ArgumentKind Kind => ArgumentKind.LocalState;

    protected override PropertyInfo ContextDataProperty { get; } =
        ParameterExpressionBuilderHelpers.ContextType.GetProperty(nameof(IResolverContext.LocalContextData))!;

    protected override MethodInfo SetStateMethod
        => throw new NotSupportedException();

    protected override MethodInfo SetStateGenericMethod
        => throw new NotSupportedException();

    public override bool CanHandle(ParameterInfo parameter)
    {
        if (parameter.Name?.EqualsOrdinal("id") ?? false)
        {
            return true;
        }

        if (parameter.Position != 0 || parameter.Member is not MethodInfo method)
        {
            return false;
        }

        if (!method.IsDefined(typeof(NodeResolverAttribute), true))
        {
            return false;
        }

        foreach (var candidate in method.GetParameters())
        {
            if (candidate.Name?.EqualsOrdinal("id") ?? false)
            {
                return false;
            }
        }

        return true;
    }

    protected override string? GetKey(ParameterInfo parameter)
        => WellKnownContextData.InternalId;

    public static NodeIdParameterExpressionBuilder Instance { get; } = new();
}
