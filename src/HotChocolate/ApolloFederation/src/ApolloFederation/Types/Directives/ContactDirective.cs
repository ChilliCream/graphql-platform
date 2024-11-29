using static HotChocolate.ApolloFederation.FederationTypeNames;
using static HotChocolate.ApolloFederation.Properties.FederationResources;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// <code>
/// directive @contact(
///   "Contact title of the subgraph owner"
///   name: String!
///   "URL where the subgraph's owner can be reached"
///   url: String
///   "Other relevant notes can be included here; supports markdown links"
///   description: String
/// ) on SCHEMA
/// </code>
///
/// Contact schema directive can be used to provide team contact information to your subgraph
/// schema. This information is automatically parsed and displayed by Apollo Studio.
/// See <see href="https://www.apollographql.com/docs/graphos/graphs/federated-graphs/#contact-info-for-subgraphs">
/// Subgraph Contact Information</see> for additional details.
///
///
/// <example>
/// <code>
/// schema
///   @contact(
///     description: "send urgent issues to [#oncall](https://yourteam.slack.com/archives/oncall)."
///     name : "My Team Name", url : "https://myteam.slack.com/archives/teams-chat-room-url") {
///   query: Query
/// }
/// </code>
/// </example>
/// </summary>
[DirectiveType(ContactDirective_Name, DirectiveLocation.Schema)]
[GraphQLDescription(ContactDirective_Description)]
public sealed class ContactDirective
{
    /// <summary>
    /// Initializes new instance of <see cref="ContactDirective"/>
    /// </summary>
    /// <param name="name">
    /// Contact title of the subgraph owner
    /// </param>
    /// <param name="url">
    /// URL where the subgraph's owner can be reached
    /// </param>
    /// <param name="description">
    /// Other relevant notes can be included here; supports markdown links
    /// </param>
    public ContactDirective(string name, string? url, string? description)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        Name = name;
        Url = url;
        Description = description;
    }

    /// <summary>
    /// Gets the contact title of the subgraph owner.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the url where the subgraph's owner can be reached.
    /// </summary>
    public string? Url { get; }

    /// <summary>
    /// Gets other relevant notes about subgraph contact information. Can include markdown links.
    /// </summary>
    public string? Description { get; }
}
