using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Internal;

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

    public bool CanHandle(ParameterDescriptor parameter)
        => typeof(Path) == parameter.Type;

    public IParameterBinding Create(ParameterDescriptor parameter)
        => this;

    public T Execute<T>(IResolverContext context)
    {
        Debug.Assert(typeof(T) == typeof(Path));
        var path = context.Path;
        return Unsafe.As<Path, T>(ref path);
    }
}
