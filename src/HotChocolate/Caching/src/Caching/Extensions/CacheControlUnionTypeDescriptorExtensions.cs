using System;
using HotChocolate.Caching;

namespace HotChocolate.Types;

public static class CacheControlUnionTypeDescriptorExtensions
{
    /// <summary>
    /// Specifies the caching rules for this union type.
    /// </summary>
    /// <param name="descriptor">
    /// The <see cref="IUnionTypeDescriptor"/>.
    /// </param>
    /// <param name="maxAge">
    /// The maximum time, in Milliseconds, fields of this
    /// type should be cached.
    /// </param>
    /// <param name="scope">
    /// The scope of fields of this type.
    /// </param>
    public static IUnionTypeDescriptor CacheControl(
        this IUnionTypeDescriptor descriptor,
        int? maxAge = null, CacheControlScope? scope = null)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return descriptor.Directive(
            new CacheControlDirective(maxAge, scope));
    }
}
