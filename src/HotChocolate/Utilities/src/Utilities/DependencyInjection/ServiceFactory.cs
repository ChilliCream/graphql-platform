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

public sealed class ServiceFactory
{
    private static readonly IServiceProvider _empty = EmptyServiceProvider.Instance;
    private readonly ConcurrentDictionary<Type, ObjectFactory> _factories = new();
         
    public IServiceProvider? Services { get; set; }

#if NET6_0_OR_GREATER
    public object CreateInstance([DynamicallyAccessedMembers(PublicConstructors)] Type type)
#else
    public object CreateInstance(Type type)
#endif
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        try
        {
            var factory = _factories.GetOrAdd(type, CreateFactory);
            return factory(Services ?? _empty, null);
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
