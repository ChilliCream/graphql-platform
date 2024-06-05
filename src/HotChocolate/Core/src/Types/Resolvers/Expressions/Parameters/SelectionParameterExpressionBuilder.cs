using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution.Processing;
using HotChocolate.Internal;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class SelectionParameterExpressionBuilder()
    : LambdaParameterExpressionBuilder<IPureResolverContext, object>(ctx => ctx.Selection)
    , IParameterBindingFactory
    , IParameterBinding
{
    public override ArgumentKind Kind => ArgumentKind.Selection;

    public override bool CanHandle(ParameterInfo parameter)
        => typeof(ISelection).IsAssignableFrom(parameter.ParameterType);

    public override Expression Build(ParameterExpressionBuilderContext context)
        => Expression.Convert(base.Build(context), context.Parameter.ParameterType);

    public IParameterBinding Create(ParameterBindingContext context)
        => this;

    public T Execute<T>(IResolverContext context)
        => (T)(object)context.Selection;

    public T Execute<T>(IPureResolverContext context)
        => (T)(object)context.Selection;
}
