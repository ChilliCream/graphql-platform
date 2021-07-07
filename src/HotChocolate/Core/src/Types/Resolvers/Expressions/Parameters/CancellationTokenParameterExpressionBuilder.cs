using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using static HotChocolate.Resolvers.Expressions.Parameters.ParameterExpressionBuilderHelpers;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class CancellationTokenParameterExpressionBuilder : IParameterExpressionBuilder
    {
        private static readonly PropertyInfo _cancellationToken;

        static CancellationTokenParameterExpressionBuilder()
        {
            _cancellationToken = ContextType.GetProperty(nameof(IResolverContext.RequestAborted))!;
            Debug.Assert(_cancellationToken is not null!, "RequestAborted property is missing." );
        }

        public ArgumentKind Kind => ArgumentKind.DocumentSyntax;

        public bool IsPure => false;

        public bool CanHandle(ParameterInfo parameter, Type source)
            => typeof(CancellationToken) == parameter.ParameterType;

        public Expression Build(ParameterInfo parameter, Type source, Expression context)
            => Expression.Property(context, _cancellationToken);
    }
}
