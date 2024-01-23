using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
#if NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Utilities.Properties;
#if NET6_0_OR_GREATER
using static System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;
#endif

namespace HotChocolate.Utilities;

/// <summary>
/// The activator helper compiles a factory delegate for types to resolver their
/// dependencies against a <see cref="IServiceProvider" />.
/// </summary>
internal static class ActivatorHelper
{
    private static readonly MethodInfo _getService =
        typeof(IServiceProvider).GetMethod(nameof(IServiceProvider.GetService))!;

    private static readonly ConcurrentDictionary<Type, CreateServiceDelegate> _cache = new();

#if NET6_0_OR_GREATER
    public static CreateServiceDelegate<TService> CompileFactory<
        [DynamicallyAccessedMembers(PublicConstructors)] TService>()
        => CompileFactory<TService>(typeof(TService));
#else
    public static CreateServiceDelegate<TService> CompileFactory<TService>()
        => CompileFactory<TService>(typeof(TService));
#endif


#if NET6_0_OR_GREATER
    public static CreateServiceDelegate<TService> CompileFactory<TService>(
        [DynamicallyAccessedMembers(PublicConstructors)]
        Type implementation)
#else
    public static CreateServiceDelegate<TService> CompileFactory<TService>(
        Type implementation)
#endif
    {
        if (implementation == null)
        {
            throw new ArgumentNullException(nameof(implementation));
        }

        return s => (TService)CompileFactory(implementation).Invoke(s)!;
    }

#if NET6_0_OR_GREATER
    public static CreateServiceDelegate CompileFactory(
        [DynamicallyAccessedMembers(PublicConstructors)]
        Type type)
#else
    public static CreateServiceDelegate CompileFactory(
        Type type)
#endif
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return _cache.GetOrAdd(
            type,
            _ =>
            {
                var services = Expression.Parameter(typeof(IServiceProvider));
                var newInstance = CreateNewInstance(type, services);
                return Expression.Lambda<CreateServiceDelegate>(newInstance, services).Compile();
            });
    }

#if NET6_0_OR_GREATER
    private static NewExpression CreateNewInstance(
        [DynamicallyAccessedMembers(PublicConstructors)]
        Type type,
        ParameterExpression services)
#else
    private static NewExpression CreateNewInstance(
        Type type,
        ParameterExpression services)
#endif
    {
        var constructor = ResolveConstructor(type);
        var arguments = CreateParameters(
            constructor.GetParameters(),
            services);
        return Expression.New(constructor, arguments);
    }

#if NET6_0_OR_GREATER
    internal static ConstructorInfo ResolveConstructor(
        [DynamicallyAccessedMembers(PublicConstructors)]
        Type type)
#else
    internal static ConstructorInfo ResolveConstructor(
        Type type)
#endif
    {
        if (type is { IsClass: false, IsValueType: false, } || type.IsAbstract)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    UtilityResources.ActivatorHelper_AbstractTypeError,
                    type.FullName));
        }

        var constructors = type
            .GetConstructors()
            .Where(t => t.IsPublic)
            .ToArray();

        if (constructors.Length == 1)
        {
            return constructors[0];
        }

        return constructors
            .OrderBy(c => c.GetParameters().Length)
            .First();
    }

    private static IEnumerable<Expression> CreateParameters(
        IEnumerable<ParameterInfo> parameters,
        Expression services)
    {
        foreach (var parameter in parameters)
        {
            yield return Expression.Convert(
                Expression.Call(
                    services,
                    _getService,
                    Expression.Constant(parameter.ParameterType)),
                parameter.ParameterType);
        }
    }
}
