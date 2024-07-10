using System;
using System.Collections.Concurrent;
#if NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Globalization;
using HotChocolate.Utilities.Properties;
using Microsoft.Extensions.DependencyInjection;
#if NET6_0_OR_GREATER
using static System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;
#endif

namespace HotChocolate.Utilities;

public static class ServiceFactory
{
    private static readonly ConcurrentDictionary<Type, ObjectFactory> _factories = new();

#if NET6_0_OR_GREATER
    public static object CreateInstance(
        IServiceProvider services,
        [DynamicallyAccessedMembers(PublicConstructors)] Type type)
#else
    public static object CreateInstance(IServiceProvider services, Type type)
#endif
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }
        
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        try
        {
            var factory = _factories.GetOrAdd(type, CreateFactory);
            return factory(services, null);
        }
        catch (Exception ex)
        {
            throw new ServiceException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    UtilityResources.ServiceFactory_CreateInstanceFailed,
                    type.FullName),
                ex);
        }
        
        static ObjectFactory CreateFactory(Type instanceType)
            => ActivatorUtilities.CreateFactory(instanceType, Array.Empty<Type>());
    }
}
