namespace HotChocolate.ApolloFederation;

public sealed class Contact
{
    /// <summary>
    /// Initializes new instance of <see cref="Contact"/>
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
    public Contact(string name, string? url, string? description)
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
}
