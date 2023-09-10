using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class FieldSyntaxParameterExpressionBuilder
    : LambdaParameterExpressionBuilder<IPureResolverContext, FieldNode>
{
    public FieldSyntaxParameterExpressionBuilder()
        : base(ctx => ctx.Selection.SyntaxNode)
    {
    }

    public override ArgumentKind Kind => ArgumentKind.FieldSyntax;

    public override bool CanHandle(ParameterInfo parameter)
        => typeof(FieldNode) == parameter.ParameterType;
}
