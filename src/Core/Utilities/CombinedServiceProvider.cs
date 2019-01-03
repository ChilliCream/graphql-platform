using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace HotChocolate.Utilities
{
    internal sealed class CombinedServiceProvider
        : IServiceProvider
    {
        private readonly IServiceProvider _first;
        private readonly IServiceProvider _second;

        public CombinedServiceProvider(
            IServiceProvider first,
            IServiceProvider second)
        {
            _first = first ?? throw new ArgumentNullException(nameof(first));
            _second = second ?? throw new ArgumentNullException(nameof(second));
        }

        public object GetService(Type serviceType)
        {
            TypeInfo serviceTypeInfo = serviceType.GetTypeInfo();

            if (serviceTypeInfo.IsGenericType
                && typeof(IEnumerable).GetTypeInfo()
                    .IsAssignableFrom(serviceTypeInfo)
                && typeof(IEnumerable<>) == serviceTypeInfo
                    .GetGenericTypeDefinition())
            {
                object result = _first.GetService(serviceType);

                if (result != null && result is Array array && array.Length > 0)
                {
                    return result;
                }

                return _second.GetService(serviceType);
            }

            return _first.GetService(serviceType)
                ?? _second.GetService(serviceType);
        }
    }
}
