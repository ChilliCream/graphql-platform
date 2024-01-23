using System;
using System.Diagnostics.CodeAnalysis;
#if NET6_0_OR_GREATER
using static System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;
#endif

#nullable enable

namespace HotChocolate.Utilities;

public static class ServiceProviderExtensions
{
    public static IServiceProvider Include(
        this IServiceProvider first,
        IServiceProvider second) =>
        new CombinedServiceProvider(first, second);

#if NET6_0_OR_GREATER
    public static T? GetOrCreateService<T>(
        this IServiceProvider services,
        [DynamicallyAccessedMembers(PublicConstructors)] Type type)
#else
    public static T? GetOrCreateService<T>(
        this IServiceProvider services,
        Type type)
#endif
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (services.GetService(type) is T s)
        {
            return s;
        }

        return CreateInstance<T>(services, type);
    }

#if NET6_0_OR_GREATER
    public static bool TryGetOrCreateService<T>(
        this IServiceProvider services,
        [DynamicallyAccessedMembers(PublicConstructors)] Type type,
        [NotNullWhen(true)] out T service)
#else
    public static bool TryGetOrCreateService<T>(
        this IServiceProvider services,
        Type type,
        [NotNullWhen(true)] out T service)
#endif
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (services.GetService(type) is T s)
        {
            service = s;
            return true;
        }

        return TryCreateInstance(services, type, out service);
    }

#if NET6_0_OR_GREATER
    public static T? CreateInstance<T>(
        this IServiceProvider services,
        [DynamicallyAccessedMembers(PublicConstructors)] Type type)
#else
    public static T? CreateInstance<T>(this IServiceProvider services, Type type)
#endif
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        var factory = new ServiceFactory { Services = services, };
        if (factory.CreateInstance(type) is T casted)
        {
            return casted;
        }
        return default;
    }

#if NET6_0_OR_GREATER
    public static bool TryCreateInstance<T>(
        this IServiceProvider services,
        [DynamicallyAccessedMembers(PublicConstructors)] Type type,
        [NotNullWhen(true)] out T service)
#else
    public static bool TryCreateInstance<T>(
        this IServiceProvider services,
        Type type,
        [NotNullWhen(true)] out T service)
#endif
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        var factory = new ServiceFactory { Services = services, };
        if (factory.CreateInstance(type) is T casted)
        {
            service = casted;
            return true;
        }

        service = default!;
        return false;
    }

    public static bool TryGetService(
        this IServiceProvider services,
        Type type,
        out object? service)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        try
        {
            service = services.GetService(type);
            return service is not null;
        }
#pragma warning disable CA1031
        catch
        {
            // azure functions does not honor the interface and throws if the service
            // is not known.
            service = null;
            return false;
        }
#pragma warning restore CA1031
    }
}
