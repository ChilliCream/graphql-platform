using HotChocolate.ApolloFederation.Properties;
using static HotChocolate.ApolloFederation.FederationTypeNames;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// Object representation of @link directive.
/// </summary>
[DirectiveType(LinkDirective_Name, DirectiveLocation.Schema, IsRepeatable = true)]
[GraphQLDescription(FederationResources.LinkDirective_Description)]
public sealed class LinkDirective
{
    /// <summary>
    /// Initializes new instance of <see cref="LinkDirective"/>
    /// </summary>
    /// <param name="url">
    /// Url of specification to be imported
    /// </param>
    /// <param name="import">
    /// Optional list of imported elements.
    /// </param>
    public LinkDirective(Uri url, IReadOnlySet<string>? import)
    {
        Url = url;
        Import = import;
    }

    /// <summary>
    /// Gets imported specification url.
    /// </summary>
    [GraphQLType<NonNullType<StringType>>]
    [GraphQLDescription(FederationResources.LinkDirective_Url_Description)]
    public Uri Url { get; }

    /// <summary>
    /// Gets optional list of imported element names.
    /// </summary>
    [GraphQLDescription(FederationResources.LinkDirective_Import_Description)]
    public IReadOnlySet<string>? Import { get; }
}
