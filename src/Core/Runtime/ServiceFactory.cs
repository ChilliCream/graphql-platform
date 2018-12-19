using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HotChocolate.Runtime
{
    public class ServiceFactory
        : IServiceProvider
    {
        public IServiceProvider Services { get; set; }

        public object CreateInstance(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            object service = Services?.GetService(type);
            if (service != null)
            {
                return service;
            }

#if NETSTANDARD1_4
            if (type.GetTypeInfo().IsInterface || type.GetTypeInfo().IsAbstract)
#else
            if (type.IsInterface || type.IsAbstract)
#endif
            {
                return null;
            }

            FactoryInfo factoryInfo = CreateFactoryInfo(Services, type);
            ParameterInfo[] parameters =
                factoryInfo.Constructor.GetParameters();
            object[] arguments = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                arguments[i] =
                    factoryInfo.Arguments[parameters[i].ParameterType];
            }

            return factoryInfo.Constructor.Invoke(arguments);
        }

        object IServiceProvider.GetService(Type serviceType) =>
            CreateInstance(serviceType);

        private FactoryInfo CreateFactoryInfo(
            IServiceProvider services,
            Type type)
        {
#if NETSTANDARD1_4
            ConstructorInfo[] constructors = type.GetTypeInfo()
                .DeclaredConstructors
                .Where(t => t.IsPublic && !t.IsAbstract)
                .ToArray();
#else
            ConstructorInfo[] constructors = type
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance);
#endif

            if (constructors.Length == 0)
            {
                throw new InvalidOperationException(
                    $"The instance type `{type.FullName}` " +
                    "must have at least on public constructor.");
            }

            if (services == null)
            {
                ConstructorInfo constructor = constructors
                    .FirstOrDefault(t => !t.GetParameters().Any());
                if (constructor == null)
                {
                    throw new InvalidOperationException(
                        $"The instance type `{type.FullName}` " +
                        "must have a parameterless constructor .");
                }
                return new FactoryInfo(type, constructor);
            }

            return GetBestMatchingConstructor(
                services, type, constructors);
        }

        private FactoryInfo GetBestMatchingConstructor(
            IServiceProvider services,
            Type type,
            ConstructorInfo[] constructors)
        {
            foreach (ConstructorInfo constructor in constructors
                .OrderByDescending(t => t.GetParameters().Length))
            {
                var factoryInfo = new FactoryInfo(type, constructor);
                if (TryResolveParameters(services, factoryInfo))
                {
                    return factoryInfo;
                }
            }

            return null;
        }

        private bool TryResolveParameters(
            IServiceProvider services,
            FactoryInfo factoryInfo)
        {
            foreach (ParameterInfo parameter in factoryInfo
                .Constructor.GetParameters())
            {
                if (!factoryInfo.Arguments.ContainsKey(parameter.ParameterType))
                {
                    object obj = services.GetService(parameter.ParameterType);
                    if (obj != null)
                    {
                        factoryInfo.Arguments.Add(parameter.ParameterType, obj);
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private class FactoryInfo
        {
            public FactoryInfo(Type type, ConstructorInfo constructor)
            {
                Type = type
                    ?? throw new ArgumentNullException(nameof(type));
                Constructor = constructor
                    ?? throw new ArgumentNullException(nameof(constructor));
                Arguments = new Dictionary<Type, object>();
            }

            public Type Type { get; }

            public ConstructorInfo Constructor { get; }

            public Dictionary<Type, object> Arguments { get; }
        }
    }
}
