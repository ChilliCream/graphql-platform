using System.Reflection;
using HotChocolate.Internal;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class PathParameterExpressionBuilder
    : LambdaParameterExpressionBuilder<IPureResolverContext, Path>
{
    public PathParameterExpressionBuilder()
        : base(ctx => ctx.Path)
    {
    }

    public override ArgumentKind Kind => ArgumentKind.Custom;

    public override bool CanHandle(ParameterInfo parameter)
        => parameter.ParameterType == typeof(Path);
}
