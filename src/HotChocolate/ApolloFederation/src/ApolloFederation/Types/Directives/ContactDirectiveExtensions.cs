namespace HotChocolate.ApolloFederation;

public static class ContactDirectiveExtensions
{
    /// <summary>
    /// Applies @contact directive which can be used to prpvode team contact information to your subgraph schema.
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
    /// <param name="descriptor">
    /// The object type descriptor on which this directive shall be annotated.
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
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="name"/> is <c>null</c>.
    /// </exception>
    public static ISchemaTypeDescriptor Contact(
        this ISchemaTypeDescriptor descriptor,
        string name, string? url = null,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrEmpty(name);
        return descriptor.Directive(new ContactDirective(name, url, description));
    }
}