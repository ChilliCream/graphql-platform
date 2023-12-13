using System.Diagnostics.CodeAnalysis;
using HotChocolate.Skimmed;
using HotChocolate.Utilities;
using static HotChocolate.Fusion.FusionDirectiveArgumentNames;
using DirectiveLocation = HotChocolate.Skimmed.DirectiveLocation;
using IHasDirectives = HotChocolate.Skimmed.IHasDirectives;

namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Represents the runtime value of
/// `directive @transport(subgraph: Name!, kind: String!, location: URI!, group: String) ON SCHEMA`.
/// </summary>
internal sealed class TransportDirective
{
    /// <summary>
    /// Creates a new instance of <see cref="TransportDirective"/>.
    /// </summary>
    /// <param name="subgraph">The name of the subgraph.</param>
    /// <param name="kind">The kind of transport.</param>
    /// <param name="location">The URI location for the transport.</param>
    /// <param name="group">Optional group for the transport.</param>
    public TransportDirective(string subgraph, TransportKind kind, Uri location, string? group = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(subgraph);
        ArgumentException.ThrowIfNullOrEmpty(kind);

        Subgraph = subgraph;
        Kind = kind;
        Location = location;
        Group = group;
    }

    /// <summary>
    /// Gets the name of the subgraph.
    /// </summary>
    public string Subgraph { get; }

    /// <summary>
    /// Gets the kind of transport.
    /// </summary>
    public TransportKind Kind { get; }

    /// <summary>
    /// Gets the URI location for the transport.
    /// </summary>
    public Uri Location { get; }

    /// <summary>
    /// Gets the optional group for the transport.
    /// </summary>
    public string? Group { get; }

    /// <summary>
    /// Creates a <see cref="Directive"/> from this <see cref="TransportDirective"/>.
    /// </summary>
    /// <param name="context">The fusion type context that provides the directive names.</param>
    /// <returns></returns>
    public Directive ToDirective(IFusionTypeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        
        var args = Group is null ? new Argument[3] : new Argument[4];
        
        args[0] = new Argument(SubgraphArg, Subgraph);
        args[1] = new Argument(KindArg, Kind);
        args[2] = new Argument(LocationArg, Location.ToString());

        if (Group is not null)
        {
            args[3] = new Argument(GroupArg, Group);
        }

        return new Directive(context.TransportDirective, args);
    }

    /// <summary>
    /// Tries to parse a <see cref="TransportDirective"/> from a <see cref="Directive"/>.
    /// </summary>
    /// <param name="directiveNode">The directive node that shall be parsed.</param>
    /// <param name="context">The fusion type context that provides the directive names.</param>
    /// <param name="directive">The parsed directive.</param>
    /// <returns>
    /// <c>true</c> if the directive could be parsed; otherwise, <c>false</c>.
    /// </returns>
    public static bool TryParse(
        Directive directiveNode,
        IFusionTypeContext context,
        [NotNullWhen(true)] out TransportDirective? directive)
    {
        ArgumentNullException.ThrowIfNull(directiveNode);
        ArgumentNullException.ThrowIfNull(context);

        if (!directiveNode.Name.EqualsOrdinal(context.TransportDirective.Name))
        {
            directive = null;
            return false;
        }

        var subgraph = directiveNode.Arguments.GetValueOrDefault(SubgraphArg)?.ExpectStringLiteral();
        var kind = directiveNode.Arguments.GetValueOrDefault(KindArg)?.ExpectStringLiteral();
        var location = directiveNode.Arguments.GetValueOrDefault(LocationArg)?.ExpectStringLiteral();
        var group = directiveNode.Arguments.GetValueOrDefault(GroupArg)?.ExpectStringLiteral();

        if (subgraph is null || kind is null || location is null)
        {
            directive = null;
            return false;
        }

        directive = new TransportDirective(
            subgraph.Value,
            new TransportKind(kind.Value),
            new Uri(location.Value),
            group?.Value);
        return true;
    }

    /// <summary>
    /// Gets all @transport directives from the specified member.
    /// </summary>
    /// <param name="member">The member that shall be checked.</param>
    /// <param name="context">The fusion type context that provides the directive names.</param>
    /// <returns>Returns all @transport directives.</returns>
    public static IEnumerable<TransportDirective> GetAllFrom(
        IHasDirectives member,
        IFusionTypeContext context)
    {
        foreach (var directive in member.Directives[context.TransportDirective.Name])
        {
            if (TryParse(directive, context, out var transportDirective))
            {
                yield return transportDirective;
            }
        }
    }

    /// <summary>
    /// Checks if the specified member has a @transport directive.
    /// </summary>
    /// <param name="member">The member that shall be checked.</param>
    /// <param name="context">The fusion type context that provides the directive names.</param>
    /// <returns>
    /// <c>true</c> if the member has a @transport directive; otherwise, <c>false</c>.
    /// </returns>
    public static bool ExistsIn(IHasDirectives member, IFusionTypeContext context)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(context);

        return member.Directives.ContainsName(context.TransportDirective.Name);
    }

    /// <summary>
    /// Creates the transport directive type.
    /// </summary>
    public static DirectiveType CreateType()
    {
        var nameType = new MissingType(FusionTypeBaseNames.Name);
        var uriType = new MissingType(FusionTypeBaseNames.Uri);

        var directiveType = new DirectiveType(FusionTypeBaseNames.TransportDirective)
        {
            Locations = DirectiveLocation.Schema,
            IsRepeatable = true,
            Arguments =
            {
                new InputField(SubgraphArg, new NonNullType(nameType)),
                new InputField(KindArg, new NonNullType(nameType)),
                new InputField(LocationArg, new NonNullType(uriType)),
                new InputField(GroupArg, nameType)
            },
            ContextData =
            {
                [WellKnownContextData.IsFusionType] = true
            }
        };

        return directiveType;
    }
    
    /// <summary>
    /// Creates a transport directive for a GraphQL over HTTP endpoint.
    /// </summary>
    /// <param name="subgraph">
    /// The name of the subgraph.
    /// </param>
    /// <param name="location">
    /// The URI location for the transport.
    /// </param>
    /// <param name="group">
    /// Optional group for the transport.
    /// </param>
    /// <returns>
    /// Returns a transport directive for a GraphQL over HTTP endpoint.
    /// </returns>
    public static TransportDirective CreateHttp(string subgraph, Uri location, string? group = null)
        => new(subgraph, TransportKind.Http, location, group);
    
    /// <summary>
    /// Creates a transport directive for a GraphQL over HTTP endpoint.
    /// </summary>
    /// <param name="subgraph">
    /// The name of the subgraph.
    /// </param>
    /// <param name="location">
    /// The URI location for the transport.
    /// </param>
    /// <param name="group">
    /// Optional group for the transport.
    /// </param>
    /// <returns></returns>
    public static TransportDirective CreateWebsocket(string subgraph, Uri location, string? group = null)
        => new(subgraph, TransportKind.WebSocket, location, group);
}