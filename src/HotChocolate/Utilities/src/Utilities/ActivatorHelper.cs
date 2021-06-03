using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Utilities.Properties;

namespace HotChocolate.Utilities
{
    /// <summary>
    /// The activator helper compiles a factory delegate for types to resolver their
    /// dependencies against a <see cref="IServiceProvider" />.
    /// </summary>
    internal static class ActivatorHelper
    {
        private static readonly MethodInfo _getService =
            typeof(IServiceProvider).GetMethod(nameof(IServiceProvider.GetService))!;

        private static readonly ConcurrentDictionary<Type, CreateServiceDelegate> _cache = new();

        public static CreateServiceDelegate<TService> CompileFactory<TService>() =>
            CompileFactory<TService>(typeof(TService));

        public static CreateServiceDelegate<TService> CompileFactory<TService>(Type implementation)
        {
            if (implementation == null)
            {
                throw new ArgumentNullException(nameof(implementation));
            }

            return s => (TService)CompileFactory(implementation).Invoke(s)!;
        }

        public static CreateServiceDelegate CompileFactory(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return _cache.GetOrAdd(type, _ =>
            {
                ParameterExpression services = Expression.Parameter(typeof(IServiceProvider));
                NewExpression newInstance = CreateNewInstance(type, services);
                return Expression.Lambda<CreateServiceDelegate>(newInstance, services).Compile();
            });
        }

        private static NewExpression CreateNewInstance(
            Type type,
            ParameterExpression services)
        {
            ConstructorInfo constructor = ResolveConstructor(type);
            IEnumerable<Expression> arguments = CreateParameters(
                constructor.GetParameters(), services);
            return Expression.New(constructor, arguments);
        }

        private static ConstructorInfo ResolveConstructor(Type type)
        {
            if ((!type.IsClass && !type.IsValueType) || type.IsAbstract)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        UtilityResources.ActivatorHelper_AbstractTypeError,
                        type.FullName));
            }

            ConstructorInfo[] constructors = type
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
            foreach (ParameterInfo parameter in parameters)
            {
                yield return Expression.Convert(Expression.Call(
                    services,
                    _getService,
                    Expression.Constant(parameter.ParameterType)),
                    parameter.ParameterType);
            }
        }
    }
}
