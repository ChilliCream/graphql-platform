using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetOperationCompiler : ResolverParameterCompilerBase
    {
        private readonly PropertyInfo _operation;

        public GetOperationCompiler()
            => _operation = ContextType.GetProperty(nameof(IResolverContext.Operation))!;

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType)
            => typeof(OperationDefinitionNode) == parameter.ParameterType;

        public override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            Type sourceType)
            => Expression.Property(context, _operation);
    }
}
