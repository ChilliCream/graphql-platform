using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class OperationDefinitionParameterExpressionBuilder()
    : LambdaParameterExpressionBuilder<OperationDefinitionNode>(ctx => ctx.Operation.Definition, isPure: true)
    , IParameterBindingFactory
    , IParameterBinding
{
    public override ArgumentKind Kind
        => ArgumentKind.OperationDefinitionSyntax;

    public override bool CanHandle(ParameterInfo parameter)
        => typeof(OperationDefinitionNode) == parameter.ParameterType;

    public IParameterBinding Create(ParameterBindingContext context)
        => this;

    public T Execute<T>(IResolverContext context)
        => (T)(object)context.Operation.Definition;
}
