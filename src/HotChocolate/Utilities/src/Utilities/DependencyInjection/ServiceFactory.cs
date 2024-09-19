using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using HotChocolate.Utilities.Properties;
using Microsoft.Extensions.DependencyInjection;
using static System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

namespace HotChocolate.Utilities;

public static class ServiceFactory
{
    private static readonly ConcurrentDictionary<Type, ObjectFactory> _factories = new();

    public static object CreateInstance(
        IServiceProvider services,
        [DynamicallyAccessedMembers(PublicConstructors)] Type type)
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
#pragma warning disable IL2067 // FIXME
            => ActivatorUtilities.CreateFactory(instanceType, []);
#pragma warning restore IL2067
    }
}
