#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// The `@specifiedBy` directive is used within the type system definition language
/// to provide a URL for specifying the behavior of custom
/// scalar definitions. The URL should point to a human-readable specification of
/// the data format, serialization, and coercion rules for the scalar. For example,
/// a GraphQL system providing a `UUID` scalar might link to
/// [RFC 4122](https://tools.ietf.org/html/rfc4122),
/// or some document defining a reasonable subset of that RFC. If a specification
/// URL is present, systems and tools that are aware of it should conform to its
/// described rules. Built-in scalar types should not provide a URL in this way.
/// </summary>
public sealed class SpecifiedByDirective(string url)
{
    public string Url { get; } = url;
}
