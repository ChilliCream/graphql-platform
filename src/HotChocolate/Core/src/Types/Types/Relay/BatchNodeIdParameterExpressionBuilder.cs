using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Relay;

internal sealed class BatchNodeIdParameterExpressionBuilder : IBatchParameterExpressionBuilder
{
    private static readonly PropertyInfo s_localContextData =
        typeof(IResolverContext).GetProperty(nameof(IResolverContext.LocalContextData))!;

    private static readonly PropertyInfo s_item =
        typeof(IReadOnlyDictionary<string, object?>).GetProperty("Item")!;

    public static BatchNodeIdParameterExpressionBuilder Instance { get; } = new();

    public ArgumentKind Kind => ArgumentKind.LocalState;

    public bool IsPure => false;

    public bool IsDefaultHandler => false;

    public bool CanHandle(ParameterInfo parameter)
        => NodeIdParameterExpressionBuilder.Instance.CanHandle(parameter);

    public Expression Build(ParameterExpressionBuilderContext context)
        => NodeIdParameterExpressionBuilder.Instance.Build(context);

    public Expression BuildElement(Expression context, Type elementType)
        => Expression.Convert(
            Expression.Property(
                Expression.Property(context, s_localContextData),
                s_item,
                Expression.Constant(WellKnownContextData.InternalId)),
            elementType);
}
