using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetFieldSyntaxCompiler : ResolverParameterCompilerBase
    {
        private readonly PropertyInfo _fieldSelection;
        private readonly PropertyInfo _fieldSyntax;

        public GetFieldSyntaxCompiler()
        {
            _fieldSelection = PureContextType.GetProperty(nameof(IPureResolverContext.Selection))!;
            _fieldSyntax = typeof(IFieldSelection).GetProperty(nameof(IFieldSelection.SyntaxNode))!;
        }

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType)
            => typeof(FieldNode) == parameter.ParameterType;

        public override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            Type sourceType)
            => Expression.Property(
                Expression.Property(context, _fieldSelection),
                _fieldSyntax);
    }
}
