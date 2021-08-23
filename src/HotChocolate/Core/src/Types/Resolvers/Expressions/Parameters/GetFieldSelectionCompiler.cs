using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetFieldSelectionCompiler<T>
        : ResolverParameterCompilerBase<T>
        where T : IResolverContext
    {
        private readonly PropertyInfo _fieldSelection;
        private readonly PropertyInfo _syntaxNode;

        public GetFieldSelectionCompiler()
        {
            _fieldSelection =
                ContextTypeInfo.GetProperty(nameof(IResolverContext.Selection))!;
             _syntaxNode =
                 _fieldSelection.PropertyType.GetProperty(nameof(IFieldSelection.SyntaxNode))!;
        }

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType) =>
            typeof(FieldNode) == parameter.ParameterType;

        public override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            Type sourceType)
        {
            return Expression.Property(
                Expression.Convert(
                    Expression.Property(context, _fieldSelection),
                    typeof(IFieldSelection)),
                _syntaxNode);
        }
    }
}
