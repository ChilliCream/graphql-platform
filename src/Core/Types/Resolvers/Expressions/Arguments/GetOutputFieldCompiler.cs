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
            ParameterInfo parameter,
            Type sourceType)
        {
            if (typeof(ObjectField) == parameter.ParameterType)
            {
                return Expression.Property(Context, _outputField);
            }
            return Expression.Convert(
                Expression.Property(Context, _outputField),
                parameter.ParameterType);
        }
    }
}
