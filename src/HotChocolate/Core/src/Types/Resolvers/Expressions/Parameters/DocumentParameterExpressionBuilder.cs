using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;
using static HotChocolate.Resolvers.Expressions.Parameters.ParameterExpressionBuilderHelpers;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class DocumentParameterExpressionBuilder : IParameterExpressionBuilder
    {
        private static readonly PropertyInfo _document;

        static DocumentParameterExpressionBuilder()
        {
            _document = ContextType.GetProperty(nameof(IResolverContext.Document))!;
            Debug.Assert(_document is not null!, "Document property is missing." );
        }

        public ArgumentKind Kind => ArgumentKind.DocumentSyntax;

        public bool IsPure => false;

        public bool CanHandle(ParameterInfo parameter)
            => typeof(DocumentNode) == parameter.ParameterType;

        public Expression Build(ParameterInfo parameter, Expression context)
            => Expression.Property(context, _document);
    }
}
