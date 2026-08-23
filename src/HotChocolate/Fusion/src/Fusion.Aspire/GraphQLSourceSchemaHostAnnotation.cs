using Aspire.Hosting.ApplicationModel;

namespace HotChocolate.Fusion.Aspire;

/// <summary>
/// Marks a resource that serves a GraphQL source schema on behalf of a schema anchor, so that
/// its restarts recompose the gateways that compose against it.
/// </summary>
internal sealed class GraphQLSourceSchemaHostAnnotation : IResourceAnnotation;
