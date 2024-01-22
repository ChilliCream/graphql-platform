using System.Collections.Generic;
using static HotChocolate.ApolloFederation.FederationTypeNames;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// Object representation of @link directive.
/// </summary>
[DirectiveType(LinkDirective_Name, DirectiveLocation.Schema, IsRepeatable = true)]
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
    public Uri Url { get; }

    /// <summary>
    /// Gets optional list of imported element names.
    /// </summary>
    public IReadOnlySet<string>? Import { get; }
}
