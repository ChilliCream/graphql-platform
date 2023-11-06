using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution.Processing;
using HotChocolate.Internal;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class SelectionParameterExpressionBuilder
    : LambdaParameterExpressionBuilder<IPureResolverContext, object>
{
    public SelectionParameterExpressionBuilder()
        : base(ctx => ctx.Selection)
    {
    }

    public override ArgumentKind Kind => ArgumentKind.Selection;

    public override bool CanHandle(ParameterInfo parameter)
        => typeof(ISelection).IsAssignableFrom(parameter.ParameterType);

    public override Expression Build(ParameterExpressionBuilderContext context)
        => Expression.Convert(base.Build(context), context.Parameter.ParameterType);
}
