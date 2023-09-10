#nullable enable
using System.Reflection;
using HotChocolate.Execution.Processing;
using HotChocolate.Internal;

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class OperationParameterExpressionBuilder
    : LambdaParameterExpressionBuilder<IPureResolverContext, IOperation>
{
    public OperationParameterExpressionBuilder()
        : base(ctx => ctx.Operation)
    {
    }

    public override ArgumentKind Kind => ArgumentKind.Operation;

    public override bool CanHandle(ParameterInfo parameter)
        => typeof(IOperation) == parameter.ParameterType;
}
