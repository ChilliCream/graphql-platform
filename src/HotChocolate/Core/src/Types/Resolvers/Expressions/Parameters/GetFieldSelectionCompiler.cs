using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetFieldSelectionCompiler<T>
        : ResolverParameterCompilerBase<T>
        where T : IResolverContext
    {
        private readonly PropertyInfo _fieldSelection;

        public GetFieldSelectionCompiler()
        {
            _fieldSelection = ContextTypeInfo.GetProperty(
                nameof(IResolverContext.FieldSelection));
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
            return Expression.Property(context, _fieldSelection);
        }
    }
}
