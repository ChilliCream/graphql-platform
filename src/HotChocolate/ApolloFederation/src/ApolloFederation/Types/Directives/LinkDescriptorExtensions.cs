using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.ApolloFederation.Types;

public static class LinkDescriptorExtensions
{
    /// <summary>
    /// Applies @link directive definitions to link the document to external schemas.
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
    ///
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
    /// <param name="builder">
    /// The GraphQL request executor builder.
    /// </param>
    /// <param name="url">
    /// Url of specification to be imported
    /// </param>
    /// <param name="imports">
    /// Optional list of imported elements.
    /// </param>
    /// <returns>
    /// Returns the object type descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="url"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddLink(
        this IRequestExecutorBuilder builder,
        string url,
        IReadOnlyList<string>? imports)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(url);
        return AddLink(builder, new Uri(url), imports);
    }

    /// <summary>
    /// Applies @link directive definitions to link the document to external schemas.
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
    ///
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
    /// <param name="builder">
    /// The GraphQL request executor builder.
    /// </param>
    /// <param name="url">
    /// Url of specification to be imported
    /// </param>
    /// <param name="imports">
    /// Optional list of imported elements.
    /// </param>
    /// <returns>
    /// Returns the object type descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="url"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddLink(
        this IRequestExecutorBuilder builder,
        Uri url,
        IReadOnlyList<string>? imports)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(url);

        builder.ConfigureSchema(
            sb =>
            {
                sb.AddSchemaConfiguration(d => d.Directive(new LinkDirective(url, imports?.ToHashSet())));
            });

        return builder;
    }
}
