using System.Reflection;
using HotChocolate.Internal;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class PathParameterExpressionBuilder()
    : LambdaParameterExpressionBuilder<Path>(ctx => ctx.Path, isPure: true)
    , IParameterBindingFactory
    , IParameterBinding
{
    public override ArgumentKind Kind
        => ArgumentKind.Custom;

    public override bool CanHandle(ParameterInfo parameter)
        => parameter.ParameterType == typeof(Path);

    public IParameterBinding Create(ParameterBindingContext context)
        => this;

    public T Execute<T>(IResolverContext context)
        => (T)(object)context.Path;
}
