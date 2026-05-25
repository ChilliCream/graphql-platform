using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Internal;

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class OperationParameterExpressionBuilder()
    : LambdaParameterExpressionBuilder<Operation>(ctx => ctx.Operation, isPure: true)
    , IParameterBindingFactory
    , IParameterBinding
{
    public override ArgumentKind Kind
        => ArgumentKind.Operation;

    public override bool CanHandle(ParameterInfo parameter)
        => typeof(IOperation) == parameter.ParameterType
            || typeof(Operation) == parameter.ParameterType;

    public bool CanHandle(ParameterDescriptor parameter)
        => typeof(IOperation) == parameter.Type
            || typeof(Operation) == parameter.Type;

    public IParameterBinding Create(ParameterDescriptor parameter)
        => this;

    public T Execute<T>(IResolverContext context)
    {
        Debug.Assert(typeof(IOperation).IsAssignableFrom(typeof(T)));
        var operation = context.Operation;
        return Unsafe.As<Operation, T>(ref operation);
    }
}
