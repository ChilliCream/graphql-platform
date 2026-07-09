using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Internal;
using HotChocolate.Language;

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

    public bool CanHandle(ParameterDescriptor parameter)
        => typeof(OperationDefinitionNode) == parameter.Type;

    public IParameterBinding Create(ParameterDescriptor parameter)
        => this;

    public T Execute<T>(IResolverContext context)
    {
        Debug.Assert(typeof(T) == typeof(OperationDefinitionNode));
        var operation = context.Operation.Definition;
        return Unsafe.As<OperationDefinitionNode, T>(ref operation);
    }
}
