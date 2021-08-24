using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers.Expressions;
using HotChocolate.Resolvers.Expressions.Parameters;

namespace HotChocolate.Internal
{
    /// <summary>
    /// This expression builder allows to map custom services as resolver parameters that do
    /// not need an attribute.
    /// </summary>
    public sealed class CustomServiceParameterExpressionBuilder<TService>
        : IParameterExpressionBuilder
    {
        private readonly Type _serviceType;
        private readonly ServiceParameterExpressionBuilder _internalBuilder = new();

        /// <summary>
        /// Initializes a new instance of
        /// <see cref="CustomServiceParameterExpressionBuilder{TService}"/>.
        /// </summary>
        public CustomServiceParameterExpressionBuilder()
        {
            _serviceType = typeof(TService);
        }

        ArgumentKind IParameterExpressionBuilder.Kind
            => _internalBuilder.Kind;

        bool IParameterExpressionBuilder.IsPure
            => _internalBuilder.IsPure;

        public bool CanHandle(ParameterInfo parameter)
            => _serviceType == parameter.ParameterType;

        public Expression Build(ParameterInfo parameter, Expression context)
            => _internalBuilder.Build(parameter, context);
    }
}
