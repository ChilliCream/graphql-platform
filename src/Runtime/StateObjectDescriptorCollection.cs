using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HotChocolate.Runtime
{
    public class StateObjectDescriptorCollection<TKey>
    {
        private readonly Dictionary<TKey, IScopedStateDescriptor<TKey>> _descriptors;

        public StateObjectDescriptorCollection(
            IEnumerable<IScopedStateDescriptor<TKey>> descriptors)
        {
            if (descriptors == null)
            {
                throw new ArgumentNullException(nameof(descriptors));
            }

            _descriptors = descriptors.ToDictionary(t => t.Key);
        }

        public bool TryGetDescriptor(
            TKey key,
            out IScopedStateDescriptor<TKey> descriptor)
        {
            return _descriptors.TryGetValue(key, out descriptor);
        }

        public Func<object> CreateFactory(
            IServiceProvider services,
            IScopedStateDescriptor<TKey> descriptor)
        {
            if (descriptor.Factory == null)
            {
                return () => descriptor.Factory(services);
            }

            FactoryInfo factoryInfo = CreateFactoryInfo(
                services, descriptor.Scope, descriptor.Type);
            ParameterInfo[] parameters =
                factoryInfo.Constructor.GetParameters();
            object[] arguments = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                arguments[i] =
                    factoryInfo.Arguments[parameters[i].ParameterType];
            }

            return () => factoryInfo.Constructor.Invoke(arguments);
        }

        private FactoryInfo CreateFactoryInfo(
            IServiceProvider services,
            ExecutionScope scope,
            Type instanceType)
        {
            ConstructorInfo[] constructors = instanceType
                .GetConstructors(BindingFlags.Public);

            if (constructors.Length == 0)
            {
                throw new InvalidOperationException(
                    $"The instance type `{instanceType.FullName}` " +
                    "must have at least on constructor.");
            }

            return GetBestMatchingConstructor(
                services, scope, instanceType, constructors);
        }

        private FactoryInfo GetBestMatchingConstructor(
            IServiceProvider services,
            ExecutionScope scope,
            Type instanceType,
            ConstructorInfo[] constructors)
        {
            foreach (ConstructorInfo constructor in constructors
                .OrderByDescending(t => t.GetParameters().Length))
            {
                var factoryInfo = new FactoryInfo(
                    instanceType, constructor);

                if (TryResolveParameters(services, scope, factoryInfo))
                {
                    return factoryInfo;
                }
            }

            return null;
        }

        private bool TryResolveParameters(
            IServiceProvider services,
            ExecutionScope scope,
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
            public FactoryInfo(Type serviceType, ConstructorInfo constructor)
            {
                ServiceType = serviceType
                    ?? throw new ArgumentNullException(nameof(serviceType));
                Constructor = constructor
                    ?? throw new ArgumentNullException(nameof(constructor));
                Arguments = new Dictionary<Type, object>();
            }

            public Type ServiceType { get; }

            public ConstructorInfo Constructor { get; }

            public Dictionary<Type, object> Arguments { get; }
        }
    }
}
