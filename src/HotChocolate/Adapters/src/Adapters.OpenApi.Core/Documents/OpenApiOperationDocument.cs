using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Adapters.OpenApi;

public sealed record OpenApiOperationDocument(
    string Id,
    string Name,
    string? Description,
    string HttpMethod,
    OpenApiRoute Route,
    ImmutableArray<OpenApiRouteSegmentParameter> QueryParameters,
    OpenApiRouteSegmentParameter? BodyParameter,
    OperationDefinitionNode OperationDefinition,
    Dictionary<string, FragmentDefinitionNode> LocalFragmentLookup,
    HashSet<string> ExternalFragmentReferences) : IOpenApiDocument;
