namespace HotChocolate.Fusion.Aspire;

internal readonly record struct SchemaEndpointConfiguration(
    string SourceSchemaName,
    ApolloFederationVersion? ApolloFederationVersion)
{
    public SchemaEndpointProtocol Protocol
        => ApolloFederationVersion is null
            ? SchemaEndpointProtocol.GraphQL
            : SchemaEndpointProtocol.ApolloFederation;

    public string DefaultPath
        => ApolloFederationVersion is null ? "/graphql/schema.graphql" : "/graphql";
}
