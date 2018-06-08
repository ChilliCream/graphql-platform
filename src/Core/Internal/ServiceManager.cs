using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Types;

namespace HotChocolate.Internal
{
    internal sealed class ServiceManager
        : IServiceProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<Type, object> _typeInstances = new Dictionary<Type, object>();

        public ServiceManager(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }
            _serviceProvider = serviceProvider;
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            if (_typeInstances.TryGetValue(serviceType, out object service))
            {
                return service;
            }

            service = _serviceProvider.GetService(serviceType);
            if (service == null && IsNamedType(serviceType))
            {
                service = TryCreateInstance(serviceType);
                if (service != null)
                {
                    _typeInstances[serviceType] = service;
                }
            }

            return service;
        }

        private bool IsNamedType(Type serviceType) =>
            typeof(INamedType).IsAssignableFrom(serviceType);

        private object TryCreateInstance(Type type)
        {
            foreach (ConstructorInfo constructor in type.GetConstructors()
                .Where(t => !t.GetParameters().Any(p => IsPrimitive(p.ParameterType)))
                .OrderByDescending(t => t.GetParameters().Length))
            {
                if (TryGetDependencies(constructor, out object[] parameters))
                {
                    return constructor.Invoke(parameters);
                }
            }
            return null;
        }

        private bool TryGetDependencies(ConstructorInfo constructor, out object[] parameterValues)
        {
            ParameterInfo[] parameters = constructor.GetParameters();
            parameterValues = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                object value = _serviceProvider.GetService(parameters[i].ParameterType);
                if (value == null)
                {
                    parameterValues = null;
                    return false;
                }
            }
            return true;
        }

        private bool IsPrimitive(Type type)
        {
            return (type.IsValueType || type == typeof(string));
        }
    }
}
