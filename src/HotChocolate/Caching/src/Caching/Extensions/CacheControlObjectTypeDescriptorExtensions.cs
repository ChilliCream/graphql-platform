using System;
using HotChocolate.Caching;

namespace HotChocolate.Types;

public static class CacheControlObjectTypeDescriptorExtensions
{
    /// <summary>
    /// Specifies the caching rules for this object type.
    /// </summary>
    /// <param name="descriptor">
    /// The <see cref="IObjectTypeDescriptor"/>.
    /// </param>
    /// <param name="maxAge">
    /// The maximum time, in Milliseconds, fields of this
    /// type should be cached.
    /// </param>
    /// <param name="scope">
    /// The scope of fields of this type.
    /// </param>
    public static IObjectTypeDescriptor CacheControl(
        this IObjectTypeDescriptor descriptor,
        int? maxAge = null, CacheControlScope? scope = null)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return descriptor.Directive(
            new CacheControlDirective(maxAge, scope));
    }

    /// <summary>
    /// Specifies the caching rules for this object type.
    /// </summary>
    /// <param name="descriptor">
    /// The <see cref="IObjectTypeDescriptor{T}"/>.
    /// </param>
    /// <param name="maxAge">
    /// The maximum time, in Milliseconds, fields of this
    /// type should be cached.
    /// </param>
    /// <param name="scope">
    /// The scope of fields of this type.
    /// </param>
    public static IObjectTypeDescriptor<T> CacheControl<T>(
        this IObjectTypeDescriptor<T> descriptor,
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
