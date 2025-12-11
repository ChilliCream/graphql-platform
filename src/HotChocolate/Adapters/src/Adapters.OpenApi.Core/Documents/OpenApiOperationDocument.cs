using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Adapters.OpenApi;

public sealed record OpenApiOperationDocument(
    string? Description,
    string HttpMethod,
    OpenApiRoute Route,
    ImmutableArray<OpenApiRouteSegmentParameter> QueryParameters,
    OpenApiRouteSegmentParameter? BodyParameter,
    OperationDefinitionNode OperationDefinition,
    Dictionary<string, FragmentDefinitionNode> LocalFragmentLookup,
    HashSet<string> ExternalFragmentReferences) : IOpenApiDocument;
