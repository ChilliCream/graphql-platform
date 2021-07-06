using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetQueryCompiler : ResolverParameterCompilerBase
    {
        private readonly PropertyInfo _query;

        public GetQueryCompiler()
            => _query = ContextType.GetProperty(nameof(IResolverContext.Document))!;

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType)
            => typeof(DocumentNode) == parameter.ParameterType;

        public override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            Type sourceType)
            => Expression.Property(context, _query);
    }
}
