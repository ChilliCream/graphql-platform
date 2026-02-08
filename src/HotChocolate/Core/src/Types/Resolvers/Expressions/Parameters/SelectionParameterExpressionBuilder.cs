using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Internal;

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class SelectionParameterExpressionBuilder()
    : LambdaParameterExpressionBuilder<object>(ctx => ctx.Selection, isPure: true)
    , IParameterBindingFactory
    , IParameterBinding
{
    public override ArgumentKind Kind
        => ArgumentKind.Selection;

    public override bool CanHandle(ParameterInfo parameter)
        => typeof(ISelection).IsAssignableFrom(parameter.ParameterType)
            || typeof(Selection).IsAssignableFrom(parameter.ParameterType);

    public bool CanHandle(ParameterDescriptor parameter)
        => typeof(ISelection).IsAssignableFrom(parameter.Type)
            || typeof(Selection).IsAssignableFrom(parameter.Type);

    public override Expression Build(ParameterExpressionBuilderContext context)
        => Expression.Convert(base.Build(context), context.Parameter.ParameterType);

    public IParameterBinding Create(ParameterDescriptor parameter)
        => this;

    public T Execute<T>(IResolverContext context)
    {
        Debug.Assert(typeof(T) == typeof(ISelection));
        var selection = context.Selection;
        return Unsafe.As<Selection, T>(ref selection);
    }
}
