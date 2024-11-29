using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.ApolloFederation.FederationContextData;

namespace HotChocolate.ApolloFederation.Types;

public static class ComposeDirectiveDescriptorExtensions
{
    /// <summary>
    /// Applies @composeDirective which is used to specify custom directives that should be exposed in the
    /// Supergraph schema. If not specified, by default, Supergraph schema excludes all custom directives.
    /// <example>
    /// extend schema @composeDirective(name: "@custom")
    ///   @link(url: "https://specs.apollo.dev/federation/v2.5", import: ["@composeDirective"])
    ///   @link(url: "https://myspecs.dev/custom/v1.0", import: ["@custom"])
    ///
    /// directive @custom on FIELD_DEFINITION
    ///
    /// type Query {
    ///   helloWorld: String! @custom
    /// }
    /// </example>
    /// </summary>
    /// <param name="builder">
    /// The GraphQL request executor builder.
    /// </param>
    /// <param name="directiveName">
    /// Name of the directive that should be preserved in the supergraph composition.
    /// </param>
    /// <returns>
    /// Returns the object type descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="directiveName"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder ExportDirective(
        this IRequestExecutorBuilder builder,
        string directiveName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(directiveName);

        builder.ConfigureSchema(
            sb =>
            {
                sb.AddSchemaConfiguration(
                    d => d.Directive(new ComposeDirective(directiveName)));
            });

        return builder;
    }

    public static IRequestExecutorBuilder ExportDirective<T>(
        this IRequestExecutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddType<T>();
        builder.AddType<ComposeDirective>();

        builder.ConfigureSchema(
            sb =>
            {
                if(!sb.ContextData.TryGetValue(ExportedDirectives, out var directiveTypes))
                {
                    directiveTypes = new List<Type>();
                    sb.ContextData[ExportedDirectives] = directiveTypes;
                }

                ((List<Type>)directiveTypes!).Add(typeof(T));
            });

        return builder;
    }
}
