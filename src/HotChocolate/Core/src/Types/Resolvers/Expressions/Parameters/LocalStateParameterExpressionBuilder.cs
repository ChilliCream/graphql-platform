using System.Reflection;
using HotChocolate.Internal;
using static HotChocolate.Resolvers.Expressions.Parameters.ParameterExpressionBuilderHelpers;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal class LocalStateParameterExpressionBuilder
    : ScopedStateParameterExpressionBuilder
{
    public override ArgumentKind Kind => ArgumentKind.LocalState;

    private static readonly PropertyInfo _localContextDataProperty =
        ContextType.GetProperty(nameof(IResolverContext.LocalContextData))!;

    protected override PropertyInfo ContextDataProperty
        => _localContextDataProperty;

    private static readonly MethodInfo _setLocalState =
        typeof(ExpressionHelper).GetMethod(
            nameof(ExpressionHelper.SetLocalState))!;
    private static readonly MethodInfo _setLocalStateGeneric =
        typeof(ExpressionHelper).GetMethod(
            nameof(ExpressionHelper.SetLocalStateGeneric))!;

    protected override MethodInfo SetStateMethod => _setLocalState;

    protected override MethodInfo SetStateGenericMethod => _setLocalStateGeneric;

    public override bool CanHandle(ParameterInfo parameter)
        => parameter.IsDefined(typeof(LocalStateAttribute));

    protected override string? GetKey(ParameterInfo parameter)
        => parameter.GetCustomAttribute<LocalStateAttribute>()!.Key;
}
