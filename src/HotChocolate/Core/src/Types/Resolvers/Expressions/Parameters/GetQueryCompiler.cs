using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetQueryCompiler<T>
        : ResolverParameterCompilerBase<T>
        where T : IResolverContext
    {
        private readonly PropertyInfo _query;

        public GetQueryCompiler()
        {
            _query = ContextTypeInfo.GetProperty(
                nameof(IResolverContext.Document));
        }

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType) =>
            typeof(DocumentNode) == parameter.ParameterType;

        public override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            Type sourceType)
        {
            return Expression.Property(context, _query);
        }
    }
}
