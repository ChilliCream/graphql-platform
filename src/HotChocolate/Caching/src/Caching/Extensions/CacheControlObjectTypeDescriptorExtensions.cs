using System.Collections.Immutable;
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
    /// The maximum time, in seconds, fields of this
    /// type should be cached.
    /// </param>
    /// <param name="scope">
    /// The scope of fields of this type.
    /// </param>
    /// <param name="sharedMaxAge">
    /// The maximum time, in seconds, fields of this
    /// type should be cached in a shared cache.
    /// </param>
    /// <param name="vary">
    /// List of headers that might affect the value of this resource.
    /// </param>
    public static IObjectTypeDescriptor CacheControl(
        this IObjectTypeDescriptor descriptor,
        int? maxAge = null,
        CacheControlScope? scope = null,
        int? sharedMaxAge = null,
        string[]? vary = null)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return descriptor.Directive(
            new CacheControlDirective(
                maxAge,
                scope,
                null,
                sharedMaxAge,
                vary?.ToImmutableArray()));
    }

    /// <summary>
    /// Specifies the caching rules for this object type.
    /// </summary>
    /// <param name="descriptor">
    /// The <see cref="IObjectTypeDescriptor{T}"/>.
    /// </param>
    /// <param name="maxAge">
    /// The maximum time, in seconds, fields of this
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
