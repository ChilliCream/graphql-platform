using System;
using System.Linq;
using System.Reflection;

namespace HotChocolate.Internal
{
    internal class ServiceFactory
    {
        private Func<Type, object> _resolveService;

        public ServiceFactory(Func<Type, object> resolveService)
        {
            _resolveService = resolveService
                ?? throw new ArgumentNullException(nameof(resolveService));
        }

        public object TryCreateInstance(Type serviceType)
        {
            foreach (ConstructorInfo constructor in serviceType.GetConstructors()
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

        private bool TryGetDependencies(
            ConstructorInfo constructor,
            out object[] parameterValues)
        {
            ParameterInfo[] parameters = constructor.GetParameters();
            parameterValues = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                object value = _resolveService(parameters[i].ParameterType);
                parameterValues[i] = value;

                if (value == null)
                {
                    parameterValues = null;
                    return false;
                }
            }

            return true;
        }

        private static bool IsPrimitive(Type type)
        {
            return (type.IsValueType || type == typeof(string));
        }
    }
}
