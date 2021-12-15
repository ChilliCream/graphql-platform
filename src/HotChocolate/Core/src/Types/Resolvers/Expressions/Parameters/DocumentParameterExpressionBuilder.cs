using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;
using static HotChocolate.Resolvers.Expressions.Parameters.ParameterExpressionBuilderHelpers;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class DocumentParameterExpressionBuilder : IParameterExpressionBuilder
{
    private static readonly PropertyInfo _document =
        ContextType.GetProperty(nameof(IResolverContext.Document))!;

    public ArgumentKind Kind => ArgumentKind.DocumentSyntax;

    public bool IsPure => false;

    public bool IsDefaultHandler => false;

    public bool CanHandle(ParameterInfo parameter)
        => typeof(DocumentNode) == parameter.ParameterType;

    public Expression Build(ParameterInfo parameter, Expression context)
        => Expression.Property(context, _document);
}
