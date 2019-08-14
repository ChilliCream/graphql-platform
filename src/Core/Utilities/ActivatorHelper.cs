using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Utilities
{
    internal static class ActivatorHelper
    {
        private static readonly ConcurrentDictionary<TypeInfo, Func<IServiceProvider, object>>
            _factories = new ConcurrentDictionary<TypeInfo, Func<IServiceProvider, object>>();

        public static Func<IServiceProvider, object> CompileFactory(
            TypeInfo typeInfo)
        {
            if (typeInfo == null)
            {
                throw new ArgumentNullException(nameof(typeInfo));
            }

            Func<IServiceProvider, object> factory;

            if (!_factories.TryGetValue(typeInfo, out factory))
            {
                ParameterExpression services =
                    Expression.Parameter(typeof(IServiceProvider));
                NewExpression newInstance = CreateNewInstance(typeInfo, services);
                factory = Expression.Lambda<Func<IServiceProvider, object>>(
                    newInstance, services).Compile();
                _factories.TryAdd(typeInfo, factory);
            }

            return factory;
        }

        public static Func<IServiceProvider, T> CompileFactory<T>(
            TypeInfo typeInfo)
        {
            if (typeInfo == null)
            {
                throw new ArgumentNullException(nameof(typeInfo));
            }

            return s => (T)CompileFactory(typeInfo).Invoke(s);
        }

        public static Func<IServiceProvider, T> CompileFactory<T>() =>
            CompileFactory<T>(typeof(T).GetTypeInfo());

        private static NewExpression CreateNewInstance(
            TypeInfo typeInfo,
            ParameterExpression services)
        {
            ConstructorInfo constructor = ResolveCunstructor(typeInfo);
            IEnumerable<Expression> arguments = CreateParameters(
                constructor.GetParameters(), services);
            return Expression.New(constructor, arguments);
        }

        private static ConstructorInfo ResolveCunstructor(TypeInfo typeInfo)
        {
            if (!typeInfo.IsClass || typeInfo.IsAbstract)
            {
                // TODO : resources
                throw new InvalidOperationException(
                    $"The type {typeInfo.FullName} is abstract and we cannot " +
                    "use it to compile a service factory from it.");
            }

            ConstructorInfo[] constructors = typeInfo.DeclaredConstructors
                .Where(t => t.IsPublic).ToArray();

            if (constructors.Length == 1)
            {
                return constructors[0];
            }

            // TODO : resources
            throw new InvalidOperationException(
                $"The specified class {typeInfo.FullName} must have exactly " +
                "one public constructor.");
        }

        private static IEnumerable<Expression> CreateParameters(
            IEnumerable<ParameterInfo> parameters,
            Expression services)
        {
            MethodInfo getService = typeof(IServiceProvider)
                .GetTypeInfo()
                .GetDeclaredMethod("GetService");

            foreach (ParameterInfo parameter in parameters)
            {
                yield return Expression.Convert(Expression.Call(
                    services,
                    getService,
                    Expression.Constant(parameter.ParameterType)),
                    parameter.ParameterType);
            }
        }
    }
}
