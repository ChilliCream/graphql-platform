using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using HotChocolate.Internal;
using HotChocolate.Properties;
using static System.Linq.Expressions.Expression;
using static HotChocolate.Utilities.NullableHelper;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class ClaimsPrincipalParameterExpressionBuilder
    : IParameterExpressionBuilder
    , IParameterBindingFactory
    , IParameterBinding
{
    public ArgumentKind Kind => ArgumentKind.Custom;

    public bool IsPure => true;

    public bool IsDefaultHandler => false;

    public bool CanHandle(ParameterInfo parameter)
        => parameter.ParameterType == typeof(ClaimsPrincipal);

    public Expression Build(ParameterExpressionBuilderContext context)
    {
        var parameter = context.Parameter;
        Expression nullableParameter = Constant(IsParameterNullable(parameter), typeof(bool));

        Expression<Func<IResolverContext, bool, ClaimsPrincipal?>> lambda =
            (ctx, nullable) => GetClaimsPrincipal(ctx, nullable);

        return Invoke(lambda, context.ResolverContext, nullableParameter);
    }

    private static ClaimsPrincipal? GetClaimsPrincipal(
        IResolverContext context,
        bool nullable)
    {
        if (context.ContextData.TryGetValue(nameof(ClaimsPrincipal), out var value) &&
            value is ClaimsPrincipal user)
        {
            return user;
        }

        if (nullable)
        {
            return null;
        }

        throw new ArgumentException(
            TypeResources.ClaimsPrincipalParameterExpressionBuilder_NoClaimsFound,
            nameof(context));
    }

    public IParameterBinding Create(ParameterBindingContext context)
        => this;

    public T Execute<T>(IResolverContext context)
        => context.GetGlobalState<T>(nameof(ClaimsPrincipal))!;
}
