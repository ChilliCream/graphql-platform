using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetCancellationTokenCompiler<T>
        : ResolverParameterCompilerBase<T>
        where T : IResolverContext
    {
        private readonly PropertyInfo _requestAborted;

        public GetCancellationTokenCompiler()
        {
            _requestAborted = ContextTypeInfo.GetDeclaredProperty(
                nameof(IResolverContext.RequestAborted));
        }

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType) =>
            typeof(CancellationToken) == parameter.ParameterType;

        public override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            Type sourceType)
        {
            return Expression.Property(context, _requestAborted);
        }
    }
}
