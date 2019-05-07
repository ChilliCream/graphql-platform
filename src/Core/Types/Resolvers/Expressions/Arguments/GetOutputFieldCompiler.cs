using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Types;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetOutputFieldCompiler<T>
        : ResolverParameterCompilerBase<T>
        where T : IResolverContext
    {
        private readonly PropertyInfo _outputField;

        public GetOutputFieldCompiler()
        {
            _outputField = ContextTypeInfo.GetProperty(
                nameof(IResolverContext.Field));
        }

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType) =>
            typeof(IOutputField).IsAssignableFrom(parameter.ParameterType);

        public override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            Type sourceType)
        {
            if (typeof(ObjectField) == parameter.ParameterType)
            {
                return Expression.Property(context, _outputField);
            }
            return Expression.Convert(
                Expression.Property(context, _outputField),
                parameter.ParameterType);
        }
    }
}
