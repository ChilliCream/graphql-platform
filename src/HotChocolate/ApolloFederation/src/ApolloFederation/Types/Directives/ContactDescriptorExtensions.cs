using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.ApolloFederation.FederationContextData;

namespace HotChocolate.ApolloFederation.Types;

public static class ContactDescriptorExtensions
{
    /// <summary>
    /// Applies @contact directive which can be used to provide team contact information to your subgraph schema.
    /// This information is automatically parsed and displayed by Apollo Studio. See
    /// <see href="https://www.apollographql.com/docs/graphos/graphs/federated-graphs/#contact-info-for-subgraphs">
    /// Subgraph Contact Information</see>
    /// for additional details.
    ///
    /// <example>
    /// schema
    ///   @contact(
    ///     description: "send urgent issues to [#oncall](https://yourteam.slack.com/archives/oncall)."
    ///     name: "My Team Name"
    ///     url: "https://myteam.slack.com/archives/teams-chat-room-url") {
    ///   query: Query
    /// }
    /// </example>
    /// </summary>
    /// <param name="builder">
    /// The GraphQL request executor builder.
    /// </param>
    /// <param name="name">
    /// Contact title of the subgraph owner
    /// </param>
    /// <param name="url">
    /// URL where the subgraph's owner can be reached
    /// </param>
    /// <param name="description">
    /// Other relevant contact notes; supports markdown links
    /// </param>
    /// <returns>
    /// Returns the object type descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="name"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddContact(
        this IRequestExecutorBuilder builder,
        string name,
        string? url = null,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        builder.ConfigureSchema(
            sb =>
            {
                if (!sb.ContextData.TryAdd(ContactMarker, 1))
                {
                    throw ThrowHelper.Contact_Not_Repeatable();
                }

                sb.AddSchemaConfiguration(d => d.Directive(new ContactDirective(name, url, description)));
            });

        return builder;
    }

    /// <summary>
    /// Applies @contact directive which can be used to provide team contact information to your subgraph schema.
    /// This information is automatically parsed and displayed by Apollo Studio. See
    /// <see href="https://www.apollographql.com/docs/graphos/graphs/federated-graphs/#contact-info-for-subgraphs">
    /// Subgraph Contact Information</see>
    /// for additional details.
    ///
    /// <example>
    /// schema
    ///   @contact(
    ///     description: "send urgent issues to [#oncall](https://yourteam.slack.com/archives/oncall)."
    ///     name: "My Team Name"
    ///     url: "https://myteam.slack.com/archives/teams-chat-room-url") {
    ///   query: Query
    /// }
    /// </example>
    /// </summary>
    /// <param name="builder">
    /// The GraphQL request executor builder.
    /// </param>
    /// <param name="contact">
    /// The contact.
    /// </param>
    /// <returns>
    /// Returns the object type descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="contact"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddContact(
        this IRequestExecutorBuilder builder,
        ContactDirective contact)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(contact);

        builder.ConfigureSchema(
            sb =>
            {
                if (!sb.ContextData.TryAdd(ContactMarker, 1))
                {
                    throw ThrowHelper.Contact_Not_Repeatable();
                }

                sb.AddSchemaConfiguration(d => d.Directive(contact));
            });

        return builder;
    }

    /// <summary>
    /// Applies @contact directive which can be used to provide team contact information to your subgraph schema.
    /// This information is automatically parsed and displayed by Apollo Studio. See
    /// <see href="https://www.apollographql.com/docs/graphos/graphs/federated-graphs/#contact-info-for-subgraphs">
    /// Subgraph Contact Information</see>
    /// for additional details.
    ///
    /// <example>
    /// schema
    ///   @contact(
    ///     description: "send urgent issues to [#oncall](https://yourteam.slack.com/archives/oncall)."
    ///     name: "My Team Name"
    ///     url: "https://myteam.slack.com/archives/teams-chat-room-url") {
    ///   query: Query
    /// }
    /// </example>
    /// </summary>
    /// <param name="builder">
    /// The GraphQL request executor builder.
    /// </param>
    /// <param name="contactResolver">
    /// A delegate to resolve the contact details from the DI or from the configuration.
    /// </param>
    /// <returns>
    /// Returns the object type descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="contactResolver"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddContact(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, ContactDirective?> contactResolver)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(contactResolver);

        builder.ConfigureSchema(
            (sp, sb) =>
            {
                var contact = contactResolver(sp.GetApplicationServices());

                if (contact is null)
                {
                    return;
                }

                if (!sb.ContextData.TryAdd(ContactMarker, 1))
                {
                    throw ThrowHelper.Contact_Not_Repeatable();
                }

                sb.AddSchemaConfiguration(d => d.Directive(contact));
            });

        return builder;
    }
}
