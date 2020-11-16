using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetOperationCompiler<T>
        : ResolverParameterCompilerBase<T>
        where T : IResolverContext
    {
        private readonly PropertyInfo _operation;

        public GetOperationCompiler()
        {
            _operation = ContextTypeInfo.GetProperty(
                nameof(IResolverContext.Operation));
        }

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType) =>
            typeof(OperationDefinitionNode) == parameter.ParameterType;

        public override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            Type sourceType)
        {
            return Expression.Property(context, _operation);
        }
    }
}
