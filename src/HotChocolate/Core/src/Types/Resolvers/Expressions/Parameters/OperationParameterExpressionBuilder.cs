#nullable enable
using System.Reflection;
using HotChocolate.Execution.Processing;
using HotChocolate.Internal;

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class OperationParameterExpressionBuilder()
    : LambdaParameterExpressionBuilder<IOperation>(ctx => ctx.Operation, isPure: true)
    , IParameterBindingFactory
    , IParameterBinding
{
    public override ArgumentKind Kind
        => ArgumentKind.Operation;

    public override bool CanHandle(ParameterInfo parameter)
        => typeof(IOperation) == parameter.ParameterType;

    public IParameterBinding Create(ParameterBindingContext context)
        => this;

    public T Execute<T>(IResolverContext context)
        => (T)context.Operation;
}
