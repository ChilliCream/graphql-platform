using System.Collections.Immutable;
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
    public static IUnionTypeDescriptor CacheControl(
        this IUnionTypeDescriptor descriptor,
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
}
