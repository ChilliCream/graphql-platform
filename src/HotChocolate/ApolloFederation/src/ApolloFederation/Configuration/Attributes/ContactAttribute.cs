using HotChocolate.Types.Descriptors;
using static HotChocolate.ApolloFederation.ThrowHelper;

namespace HotChocolate.ApolloFederation;

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
/// Contact schema directive can be used to provide team contact information to your subgraph schema. This information is automatically parsed and displayed by Apollo Studio.
/// See <see href="https://www.apollographql.com/docs/graphos/graphs/federated-graphs/#contact-info-for-subgraphs">Subgraph Contact Information</see> for additional details.
///
///
/// <example>
/// <code>
/// schema @contact(description : "send urgent issues to [#oncall](https://yourteam.slack.com/archives/oncall).", name : "My Team Name", url : "https://myteam.slack.com/archives/teams-chat-room-url"){
///   query: Query
/// }
/// </code>
/// </example>
/// </summary>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct,
    Inherited = true)]
public sealed class ContactAttribute : SchemaTypeDescriptorAttribute
{
    /// <summary>
    /// Initializes new instance of <see cref="ContactAttribute"/>
    /// </summary>
    /// <param name="name">
    /// Contact title of the subgraph owner
    /// </param>
    /// <param name="url">
    /// URL where the subgraph's owner can be reached
    /// </param>
    /// <param name="description">
    /// Other relevant contact notes; supports markdown links
    /// </param>
    public ContactAttribute(string name, string? url = null, string? description = null)
    {
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

    public override void OnConfigure(IDescriptorContext context, ISchemaTypeDescriptor descriptor, Type type)
    {
        if (string.IsNullOrEmpty(Url))
        {
            throw Contact_Name_CannotBeEmpty(type);
        }
        descriptor.Contact(Name, Url, Description);
    }
}
