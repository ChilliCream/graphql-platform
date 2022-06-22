using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

/// <summary>
/// Provides helpers for service expression builders.
/// </summary>
public static class ServiceExpressionHelper
{
    private const string _service = nameof(IPureResolverContext.Service);
    private const string _fromServicesAttribute = "FromServicesAttribute";

    private static readonly MethodInfo _getServiceMethod =
        ParameterExpressionBuilderHelpers.PureContextType.GetMethods().First(
            method => method.Name.Equals(_service, StringComparison.Ordinal) &&
                method.IsGenericMethod);

    private static readonly PropertyInfo _contextData =
        ParameterExpressionBuilderHelpers.ContextType.GetProperty(
            nameof(IResolverContext.LocalContextData))!;

    private static readonly MethodInfo _getScopedState =
        typeof(ExpressionHelper).GetMethod(nameof(ExpressionHelper.GetScopedState))!;

    private static readonly MethodInfo _getScopedStateWithDefault =
        typeof(ExpressionHelper).GetMethod(nameof(ExpressionHelper.GetScopedStateWithDefault))!;

    /// <summary>
    /// Applies the service configurations.
    /// </summary>
    public static void ApplyConfiguration(
        ParameterInfo parameter,
        ObjectFieldDescriptor descriptor,
        ServiceKind serviceKind)
    {
        if (parameter is null)
        {
            throw new ArgumentNullException(nameof(parameter));
        }

        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        switch (serviceKind)
        {
            case ServiceKind.Default:
                return;

            case ServiceKind.Synchronized:
                descriptor.Extend().Definition.IsParallelExecutable = false;
                break;

            case ServiceKind.Pooled:
                ServiceHelper.UsePooledService(descriptor.Definition, parameter.ParameterType);
                break;

            case ServiceKind.Resolver:
                ServiceHelper.UseResolverService(descriptor.Definition, parameter.ParameterType);
                return;

            default:
                throw new NotSupportedException(
                    $"Service kind `{serviceKind}` is not supported.");
        }
    }

    /// <summary>
    /// Builds the service expression.
    /// </summary>
    public static Expression Build(
        ParameterInfo parameter,
        Expression context,
        ServiceKind serviceKind)
    {
        if (parameter is null)
        {
            throw new ArgumentNullException(nameof(parameter));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        return serviceKind is ServiceKind.Default or ServiceKind.Synchronized
            ? BuildDefaultService(parameter, context)
            : BuildLocalService(parameter, context);
    }

    private static Expression BuildDefaultService(ParameterInfo parameter, Expression context)
    {
        var parameterType = parameter.ParameterType;
        var argumentMethod = _getServiceMethod.MakeGenericMethod(parameterType);
        return Expression.Call(context, argumentMethod);
    }

    private static Expression BuildLocalService(ParameterInfo parameter, Expression context)
    {
        var key = Expression.Constant(
            parameter.ParameterType.FullName ??
            parameter.ParameterType.Name,
            typeof(string));

        var contextData = Expression.Property(context, _contextData);

        var getScopedState =
            parameter.HasDefaultValue
                ? _getScopedStateWithDefault.MakeGenericMethod(parameter.ParameterType)
                : _getScopedState.MakeGenericMethod(parameter.ParameterType);

        return parameter.HasDefaultValue
            ? Expression.Call(
                getScopedState,
                context,
                contextData,
                key,
                Expression.Constant(true, typeof(bool)),
                Expression.Constant(parameter.RawDefaultValue, parameter.ParameterType))
            : Expression.Call(
                getScopedState,
                context,
                contextData,
                key,
                Expression.Constant(
                    new NullableHelper(parameter.ParameterType)
                        .GetFlags(parameter).FirstOrDefault() ?? false,
                    typeof(bool)));
    }

    public static bool TryGetServiceKind(ParameterInfo parameter, out ServiceKind kind)
    {
        if (parameter.IsDefined(typeof(ServiceAttribute)))
        {
            kind = parameter.GetCustomAttribute<ServiceAttribute>()!.Kind;
            return true;
        }

        if (parameter.GetCustomAttributesData()
            .Any(t => t.AttributeType.Name.EqualsOrdinal(_fromServicesAttribute)))
        {
            kind = ServiceKind.Default;
            return true;
        }

        kind = default;
        return false;
    }
}
