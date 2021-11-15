using System;
using HotChocolate.Types;

namespace HotChocolate.Caching;

public static class ObjectTypeDescriptorExtensions
{
    public static IObjectTypeDescriptor CacheControl(
        this IObjectTypeDescriptor descriptor,
        int? maxAge = null, CacheControlScope? scope = null,
        bool? inheritMaxAge = null)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return descriptor.Directive(new CacheControlDirective(maxAge, scope, inheritMaxAge));
    }

    public static IObjectTypeDescriptor<T> CacheControl<T>(
        this IObjectTypeDescriptor<T> descriptor,
        int? maxAge = null, CacheControlScope? scope = null,
        bool? inheritMaxAge = null)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return descriptor.Directive(new CacheControlDirective(maxAge, scope, inheritMaxAge));
    }
}