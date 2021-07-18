using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Utilities;
using static HotChocolate.Resolvers.Expressions.Parameters.ParameterExpressionBuilderHelpers;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    /// <summary>
    /// Builds parameter expressions for resolver level dependency injection.
    /// Parameters need to be annotated with the <see cref="ServiceAttribute"/> or the
    /// <c>FromServicesAttribute</c>.
    /// </summary>
    internal sealed class ServiceParameterExpressionBuilder : IParameterExpressionBuilder
    {
        private const string _service = nameof(IPureResolverContext.Service);
        private const string _fromServicesAttribute = "FromServicesAttribute";
        private static readonly MethodInfo _getServiceMethod;

        static ServiceParameterExpressionBuilder()
        {
            _getServiceMethod = PureContextType.GetMethods().First(IsServiceMethod);

            static bool IsServiceMethod(MethodInfo method)
                => method.Name.Equals(_service, StringComparison.Ordinal) &&
                   method.IsGenericMethod;
        }

        public ArgumentKind Kind => ArgumentKind.Service;

        public bool IsPure => true;

        public bool CanHandle(ParameterInfo parameter)
            => IsService(parameter);

        public Expression Build(ParameterInfo parameter, Expression context)
        {
            Type parameterType = parameter.ParameterType;
            MethodInfo argumentMethod = _getServiceMethod.MakeGenericMethod(parameterType);
            return Expression.Call(context, argumentMethod);
        }

        private static bool IsService(ParameterInfo parameter)
        {
            if (parameter.IsDefined(typeof(ServiceAttribute)))
            {
                return true;
            }

            return parameter.GetCustomAttributesData().Any(
                t => t.AttributeType.Name.EqualsOrdinal(_fromServicesAttribute));
        }
    }
}
