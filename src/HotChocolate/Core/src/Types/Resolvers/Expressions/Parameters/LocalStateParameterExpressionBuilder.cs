using System.Reflection;
using HotChocolate.Internal;
using static HotChocolate.Resolvers.Expressions.Parameters.ParameterExpressionBuilderHelpers;

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal class LocalStateParameterExpressionBuilder
    : ScopedStateParameterExpressionBuilder
{
    public override ArgumentKind Kind => ArgumentKind.LocalState;

    private static readonly PropertyInfo s_localContextDataProperty =
        ContextType.GetProperty(nameof(IResolverContext.LocalContextData))!;

    protected override PropertyInfo ContextDataProperty
        => s_localContextDataProperty;

    private static readonly MethodInfo s_setLocalState =
        typeof(ExpressionHelper).GetMethod(
            nameof(ExpressionHelper.SetLocalState))!;
    private static readonly MethodInfo s_setLocalStateGeneric =
        typeof(ExpressionHelper).GetMethod(
            nameof(ExpressionHelper.SetLocalStateGeneric))!;

    protected override MethodInfo SetStateMethod => s_setLocalState;

    protected override MethodInfo SetStateGenericMethod => s_setLocalStateGeneric;

    public override bool CanHandle(ParameterInfo parameter)
        => parameter.IsDefined(typeof(LocalStateAttribute));

    protected override string? GetKey(ParameterInfo parameter)
        => parameter.GetCustomAttribute<LocalStateAttribute>()!.Key;
}
