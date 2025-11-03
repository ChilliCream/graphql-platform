using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Internal;
using HotChocolate.Language;

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class DocumentParameterExpressionBuilder()
    : LambdaParameterExpressionBuilder<DocumentNode>(ctx => ctx.Operation.Document, isPure: true)
    , IParameterBindingFactory
    , IParameterBinding
{
    public override ArgumentKind Kind => ArgumentKind.DocumentSyntax;

    public override bool CanHandle(ParameterInfo parameter)
        => typeof(DocumentNode) == parameter.ParameterType;

    public bool CanHandle(ParameterDescriptor parameter)
        => typeof(DocumentNode) == parameter.Type;

    public IParameterBinding Create(ParameterDescriptor parameter)
        => this;

    public T Execute<T>(IResolverContext context)
    {
        Debug.Assert(typeof(T) == typeof(DocumentNode));
        var document = context.Operation.Document;
        return Unsafe.As<DocumentNode, T>(ref document);
    }
}
