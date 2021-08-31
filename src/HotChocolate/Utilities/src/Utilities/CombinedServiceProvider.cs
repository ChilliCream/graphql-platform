﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#nullable enable

namespace HotChocolate.Utilities
{
    internal sealed class CombinedServiceProvider : IServiceProvider
    {
        private const string _methodNameAny = nameof(Enumerable.Any);
        private const string _methodNameConcat = nameof(Enumerable.Concat);
        private static readonly TypeInfo _enumerableTypeInfo = typeof(Enumerable).GetTypeInfo();
        private static readonly Type _genericIEnumerableType = typeof(IEnumerable<>);
        private static readonly TypeInfo _iEnumerableTypeInfo = typeof(IEnumerable).GetTypeInfo();
        private readonly IServiceProvider _first;
        private readonly IServiceProvider _second;

        public CombinedServiceProvider(IServiceProvider first, IServiceProvider second)
        {
            _first = first ?? throw new ArgumentNullException(nameof(first));
            _second = second ?? throw new ArgumentNullException(nameof(second));
        }

        public object? GetService(Type serviceType)
        {
            if (serviceType is null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            TypeInfo serviceTypeInfo = serviceType.GetTypeInfo();

            if (serviceTypeInfo.IsGenericType &&
                _iEnumerableTypeInfo.IsAssignableFrom(serviceTypeInfo) &&
                _genericIEnumerableType == serviceTypeInfo.GetGenericTypeDefinition())
            {
                var firstResult = _first.GetService(serviceType);
                var secondResult = _second.GetService(serviceType);
                return Concat(serviceType, firstResult, secondResult);
            }

            return _first.GetService(serviceType) ?? _second.GetService(serviceType);
        }

        private static bool Any(Type enumerableType, object enumerable)
        {
            Type genericArgumentType = enumerableType
                .GetTypeInfo()
                .GenericTypeArguments[0];

            MethodInfo info = _enumerableTypeInfo
                .DeclaredMethods
                .First(m => m.Name == _methodNameAny && m.IsStatic)
                .MakeGenericMethod(genericArgumentType);

            return (bool)info.Invoke(null, new[] { enumerable })!;
        }

        private static object? Concat(
            Type enumerableType,
            object? enumerableA,
            object? enumerableB)
        {
            if (enumerableA != null && Any(enumerableType, enumerableA))
            {
                if (enumerableB != null && Any(enumerableType, enumerableB))
                {
                    Type genericArgumentType = enumerableType
                        .GetTypeInfo()
                        .GenericTypeArguments[0];

                    MethodInfo info = _enumerableTypeInfo
                        .DeclaredMethods
                        .First(m => m.Name == _methodNameConcat && m.IsStatic)
                        .MakeGenericMethod(genericArgumentType);

                    return info.Invoke(null, new[] { enumerableA, enumerableB })!;
                }

                return enumerableA;
            }

            return enumerableB;
        }
    }
}
