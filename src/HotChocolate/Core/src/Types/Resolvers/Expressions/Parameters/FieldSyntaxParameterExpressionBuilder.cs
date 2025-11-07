using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Internal;
using HotChocolate.Language;

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

    public bool CanHandle(ParameterDescriptor parameter)
        => typeof(FieldNode) == parameter.Type;

    public IParameterBinding Create(ParameterDescriptor parameter)
        => this;

    public T Execute<T>(IResolverContext context)
    {
        Debug.Assert(typeof(T) == typeof(FieldNode));
        var syntaxNode = context.Selection.SyntaxNode;
        return Unsafe.As<FieldNode, T>(ref syntaxNode);
    }
}
