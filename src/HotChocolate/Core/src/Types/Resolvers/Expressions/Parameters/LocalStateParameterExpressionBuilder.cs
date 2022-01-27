using System.Reflection;
using HotChocolate.Internal;
using static HotChocolate.Resolvers.Expressions.Parameters.ParameterExpressionBuilderHelpers;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class LocalStateParameterExpressionBuilder
    : ScopedStateParameterExpressionBuilder
{
    public override ArgumentKind Kind => ArgumentKind.LocalState;

    protected override PropertyInfo ContextDataProperty { get; } =
        ContextType.GetProperty(nameof(IResolverContext.LocalContextData))!;

    protected override MethodInfo SetStateMethod { get; } =
        typeof(ExpressionHelper).GetMethod(nameof(ExpressionHelper.SetLocalState))!;

    protected override MethodInfo SetStateGenericMethod { get; } =
        typeof(ExpressionHelper).GetMethod(nameof(ExpressionHelper.SetLocalStateGeneric))!;

    public override bool CanHandle(ParameterInfo parameter)
        => parameter.IsDefined(typeof(LocalStateAttribute));

    protected override string? GetKey(ParameterInfo parameter)
        => parameter.GetCustomAttribute<LocalStateAttribute>()!.Key;
}
