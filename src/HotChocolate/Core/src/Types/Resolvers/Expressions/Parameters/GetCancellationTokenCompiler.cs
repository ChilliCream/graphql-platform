using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetCancellationTokenCompiler : ResolverParameterCompilerBase
    {
        private readonly PropertyInfo _requestAborted;

        public GetCancellationTokenCompiler()
            => _requestAborted = ContextType.GetProperty(nameof(IResolverContext.RequestAborted))!;

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType)
            => typeof(CancellationToken) == parameter.ParameterType;

        public override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            Type sourceType)
            => Expression.Property(context, _requestAborted);
    }
}
