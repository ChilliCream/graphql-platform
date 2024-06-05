using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class OperationDefinitionParameterExpressionBuilder
    : LambdaParameterExpressionBuilder<IPureResolverContext, OperationDefinitionNode>
    , IParameterBindingFactory
    , IParameterBinding
{
    public OperationDefinitionParameterExpressionBuilder()
        : base(ctx => ctx.Operation.Definition)
    {
    }

    public override ArgumentKind Kind => ArgumentKind.OperationDefinitionSyntax;

    public override bool CanHandle(ParameterInfo parameter)
        => typeof(OperationDefinitionNode) == parameter.ParameterType;

    public IParameterBinding Create(ParameterBindingContext context)
        => this;

    public T Execute<T>(IResolverContext context)
        => (T)(object)context.Operation.Definition;

    public T Execute<T>(IPureResolverContext context)
        => (T)(object)context.Operation.Definition;
}
