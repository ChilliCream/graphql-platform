using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class OperationDefinitionParameterExpressionBuilder
    : LambdaParameterExpressionBuilder<IPureResolverContext, OperationDefinitionNode>
{
    public OperationDefinitionParameterExpressionBuilder()
        : base(ctx => ctx.Operation.Definition)
    {
    }

    public override ArgumentKind Kind => ArgumentKind.OperationDefinitionSyntax;

    public override bool CanHandle(ParameterInfo parameter)
        => typeof(OperationDefinitionNode) == parameter.ParameterType;
}
