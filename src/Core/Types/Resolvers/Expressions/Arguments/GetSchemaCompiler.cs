using System;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetSchemaCompiler<T>
        : ResolverParameterCompilerBase<T>
        where T : IResolverContext
    {
        private readonly PropertyInfo _schema;

        public GetSchemaCompiler()
        {
            _schema = ContextTypeInfo.GetProperty(
                nameof(IResolverContext.Schema));
        }

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType) => typeof(ISchema) == parameter.ParameterType;

        public override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            Type sourceType)
        {
            return Expression.Property(context, _schema);
        }
    }
}
