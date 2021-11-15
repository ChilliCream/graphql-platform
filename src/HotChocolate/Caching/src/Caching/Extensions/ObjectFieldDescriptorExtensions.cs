using System;
using HotChocolate.Types;

namespace HotChocolate.Caching;

public static class ObjectFieldDescriptorExtensions
{
    public static IObjectFieldDescriptor CacheControl(
        this IObjectFieldDescriptor descriptor,
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