using HotChocolate.Types.Descriptors;
using static HotChocolate.ApolloFederation.ThrowHelper;

namespace HotChocolate.ApolloFederation;

/// <summary>
/// <code>
/// directive @link(url: String!, import: [String]) repeatable on SCHEMA
/// </code>
///
/// The @link directive links definitions within the document to external schemas.
/// External schemas are identified by their url, which optionally ends with a name and version with
/// the following format: `{NAME}/v{MAJOR}.{MINOR}`
///
/// By default, external types should be namespaced (prefixed with namespace__, e.g. key directive
/// should be namespaced as federation__key) unless they are explicitly imported. We automatically
/// import ALL federation directives to avoid the need for namespacing.
///
/// NOTE: We currently DO NOT support full @link directive capability as it requires support for
/// namespacing and renaming imports. This functionality may be added in the future releases.
/// See @link specification for details.
/// <example>
/// extend schema @link(url: "https://specs.apollo.dev/federation/v2.5", import: ["@composeDirective"])
///
/// type Query {
///   foo: Foo!
/// }
///
/// type Foo @key(fields: "id") {
///   id: ID!
///   name: String
/// }
/// </example>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = true)]
public sealed class LinkAttribute : SchemaTypeDescriptorAttribute
{
    /// <summary>
    /// Initializes new instance of <see cref="LinkAttribute"/>
    /// </summary>
    /// <param name="url">
    /// Url of specification to be imported
    /// </param>
    public LinkAttribute(string url)
    {
        Url = url;
        Import = null;
    }

    /// <summary>
    /// Initializes new instance of <see cref="LinkAttribute"/>
    /// </summary>
    /// <param name="url">
    /// Url of specification to be imported
    /// </param>
    /// <param name="import">
    /// Optional list of imported elements.
    /// </param>
    public LinkAttribute(string url, string?[]? import)
    {
        Url = url;
        Import = import;
    }

    /// <summary>
    /// Gets imported specification url.
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// Gets optional list of imported element names.
    /// </summary>
    public string?[]? Import { get; }

    public override void OnConfigure(IDescriptorContext context, ISchemaTypeDescriptor descriptor, Type type)
    {
        if (string.IsNullOrEmpty(Url))
        {
            throw Link_Url_CannotBeEmpty(type);
        }
        descriptor.Link(Url, Import);
    }
}
