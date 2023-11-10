using HotChocolate.Language;
using static HotChocolate.Fusion.FusionDirectiveArgumentNames;

namespace HotChocolate.Fusion.Metadata;

/// <summary>
/// This base class allows to rewrite the gateway configuration before it is applied.
/// </summary>
public abstract class ConfigurationRewriter : IConfigurationRewriter
{
    /// <summary>
    /// Rewrites the gateway configuration.
    /// </summary>
    /// <param name="configuration">
    /// The gateway configuration.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns the rewritten gateway configuration.
    /// </returns>
    public virtual async ValueTask<DocumentNode> RewriteAsync(
        DocumentNode configuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var config = FusionGraphConfiguration.Load(configuration);

        SchemaDefinitionNode? schemaDefinitionNode = null;
        List<DirectiveNode>? schemaDirectives = null;

        foreach (var client in config.HttpClients)
        {
            var rewritten = await RewriteAsync(client, cancellationToken).ConfigureAwait(false);;

            if (!ReferenceEquals(rewritten, client))
            {
                var arguments = new List<ArgumentNode>();
                arguments.Add(new ArgumentNode(ClientGroupArg, rewritten.ClientName));
                arguments.Add(new ArgumentNode(SubgraphArg, rewritten.SubgraphName));
                arguments.Add(new ArgumentNode(LocationArg, rewritten.EndpointUri.ToString()));
                arguments.Add(new ArgumentNode(LocationArg, rewritten.EndpointUri.ToString()));
                arguments.Add(new ArgumentNode(KindArg, "HTTP"));
                Replace(client.SyntaxNode!, client.SyntaxNode!.WithArguments(arguments));
            }
        }

        foreach (var client in config.WebSocketClients)
        {
            var rewritten = await RewriteAsync(client, cancellationToken).ConfigureAwait(false);

            if (!ReferenceEquals(rewritten, client))
            {
                var arguments = new List<ArgumentNode>();
                arguments.Add(new ArgumentNode(ClientGroupArg, rewritten.ClientName));
                arguments.Add(new ArgumentNode(SubgraphArg, rewritten.SubgraphName));
                arguments.Add(new ArgumentNode(LocationArg, rewritten.EndpointUri.ToString()));
                arguments.Add(new ArgumentNode(KindArg, "WebSocket"));
                Replace(client.SyntaxNode!, client.SyntaxNode!.WithArguments(arguments));
            }
        }

        return configuration;

        void Replace(DirectiveNode currentDirective, DirectiveNode newDirective)
        {
            if (schemaDirectives is null)
            {
                schemaDefinitionNode ??= configuration.Definitions.OfType<SchemaDefinitionNode>().First();

                var definitions = configuration.Definitions.ToList();
                schemaDirectives = schemaDefinitionNode.Directives.ToList();

                var definitionIndex = definitions.IndexOf(schemaDefinitionNode);
                schemaDefinitionNode = schemaDefinitionNode.WithDirectives(schemaDirectives);
                definitions[definitionIndex] = schemaDefinitionNode;
                configuration = configuration.WithDefinitions(definitions);
            }

            var index = schemaDirectives.IndexOf(currentDirective);
            schemaDirectives[index] = newDirective;
        }
    }

    /// <summary>
    /// Rewrites the HTTP client configuration of a subgraph.
    /// </summary>
    /// <param name="configuration">
    /// The HTTP client configuration.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns the rewritten HTTP client configuration.
    /// </returns>
    protected virtual ValueTask<HttpClientConfiguration> RewriteAsync(
        HttpClientConfiguration configuration,
        CancellationToken cancellationToken)
        => new(configuration);

    /// <summary>
    /// Rewrites the WebSocket client configuration of a subgraph.
    /// </summary>
    /// <param name="configuration">
    /// The WebSocket client configuration.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns the rewritten WebSocket client configuration.
    /// </returns>
    protected virtual ValueTask<WebSocketClientConfiguration> RewriteAsync(
        WebSocketClientConfiguration configuration,
        CancellationToken cancellationToken)
        => new(configuration);
}
