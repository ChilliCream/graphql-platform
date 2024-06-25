#nullable enable

using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class DocumentParameterExpressionBuilder()
    : LambdaParameterExpressionBuilder<DocumentNode>(
        ctx => ctx.Operation.Document,
        isPure: true)
    , IParameterBindingFactory
    , IParameterBinding
{
    public override ArgumentKind Kind => ArgumentKind.DocumentSyntax;

    public override bool CanHandle(ParameterInfo parameter)
        => typeof(DocumentNode) == parameter.ParameterType;

    public IParameterBinding Create(ParameterBindingContext context)
        => this;

    public T Execute<T>(IResolverContext context)
        => (T)(object)context.Operation.Document;
}
