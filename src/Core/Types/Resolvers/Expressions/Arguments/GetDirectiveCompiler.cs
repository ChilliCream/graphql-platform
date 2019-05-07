using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Types;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetDirectiveCompiler
        : ResolverParameterCompilerBase<IDirectiveContext>
    {
        private readonly PropertyInfo _directive;

        public GetDirectiveCompiler()
        {
            _directive = typeof(IDirectiveContext)
                .GetTypeInfo()
                .GetDeclaredProperty(nameof(IDirectiveContext.Directive));
        }

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType) =>
            typeof(IDirective) == parameter.ParameterType;

        public override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            Type sourceType)
        {
            return Expression.Property(context, _directive);
        }
    }
}
