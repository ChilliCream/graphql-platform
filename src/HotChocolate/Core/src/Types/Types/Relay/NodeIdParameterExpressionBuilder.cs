using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Resolvers.Expressions.Parameters;
using HotChocolate.Utilities;

#nullable enable

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
        => parameter.Name?.EqualsOrdinal("id") ?? false;

    protected override string? GetKey(ParameterInfo parameter)
        => WellKnownContextData.InternalId;

    public static NodeIdParameterExpressionBuilder Instance { get; } = new();
}
