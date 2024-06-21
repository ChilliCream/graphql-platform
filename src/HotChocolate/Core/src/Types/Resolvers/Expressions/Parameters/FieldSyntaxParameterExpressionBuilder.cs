using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class FieldSyntaxParameterExpressionBuilder()
    : LambdaParameterExpressionBuilder<FieldNode>(ctx => ctx.Selection.SyntaxNode, isPure: true)
    , IParameterBindingFactory
    , IParameterBinding
{
    public override ArgumentKind Kind
        => ArgumentKind.FieldSyntax;

    public override bool CanHandle(ParameterInfo parameter)
        => typeof(FieldNode) == parameter.ParameterType;

    public IParameterBinding Create(ParameterBindingContext context)
        => this;

    public T Execute<T>(IResolverContext context)
        => (T)context.Selection.Field;
}
