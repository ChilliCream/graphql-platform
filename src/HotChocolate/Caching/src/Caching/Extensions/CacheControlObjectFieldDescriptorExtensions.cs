using System.Collections.Immutable;
using HotChocolate.Caching;

namespace HotChocolate.Types;

public static class CacheControlObjectFieldDescriptorExtensions
{
    /// <summary>
    /// Specifies the caching rules for this object field.
    /// </summary>
    /// <param name="descriptor">
    /// The <see cref="IObjectFieldDescriptor"/>.
    /// </param>
    /// <param name="maxAge">
    /// The maximum time, in seconds, fields of this
    /// type should be cached.
    /// </param>
    /// <param name="scope">
    /// The scope of fields of this type.
    /// </param>
    /// <param name="inheritMaxAge">
    /// Whether this field should inherit the <c>MaxAge</c>
    /// from its parent.
    /// </param>
    /// <param name="sharedMaxAge">
    /// The maximum time, in seconds, fields of this
    /// type should be cached in a shared cache.
    /// </param>
    /// <param name="vary">
    /// List of headers that might affect the value of this resource.
    /// </param>
    ///
    public static IObjectFieldDescriptor CacheControl(
        this IObjectFieldDescriptor descriptor,
        int? maxAge = null, CacheControlScope? scope = null,
        bool? inheritMaxAge = null,
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
                inheritMaxAge,
                sharedMaxAge,
                vary?.ToImmutableArray()));
    }
}
