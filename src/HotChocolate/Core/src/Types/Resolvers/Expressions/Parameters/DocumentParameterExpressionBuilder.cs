using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class DocumentParameterExpressionBuilder
    : LambdaParameterExpressionBuilder<IPureResolverContext, DocumentNode>
{
    public DocumentParameterExpressionBuilder()
        : base(ctx => ctx.Operation.Document)
    {
    }

    public override ArgumentKind Kind => ArgumentKind.DocumentSyntax;

    public override bool CanHandle(ParameterInfo parameter)
        => typeof(DocumentNode) == parameter.ParameterType;
}
