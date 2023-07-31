using HotChocolate.OpenApi.Models;
using HotChocolate.Types;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;

namespace HotChocolate.OpenApi;

internal sealed class OpenApiWrapperContext
{
    private readonly List<Action<IObjectTypeDescriptor>> _graphQLTypes = new();

    public OpenApiDocument OpenApiDocument { get; }
    public OpenApiSpecVersion OpenApiSpecVersion { get; }

    public Action<IObjectTypeDescriptor>? Query { get; set; }

    public Dictionary<string, Operation> Operations { get; } = new();
    public Dictionary<string, OpenApiSchema> InputTypeSchemas { get; } = new();
    public Dictionary<string, OpenApiSchema> TypeSchemas { get; } = new();

    public IReadOnlyList<Action<IObjectTypeDescriptor>> GraphQLTypes => _graphQLTypes;

    public Dictionary<string, Operation> CallbackOperations { get; } = new();
    public HashSet<string> UsedTpeNames { get; } = new();
    public Dictionary<string, OpenApiSecurityScheme> Security { get; } = new();
    public Dictionary<string, string> SanitizedMap { get; } = new();
    public OpenApiDocument[] UsedDocuments { get; }

    public OpenApiWrapperContext(OpenApiDocument openApiDocument, OpenApiSpecVersion openApiSpecVersion)
    {
        OpenApiDocument = openApiDocument;
        OpenApiSpecVersion = openApiSpecVersion;
    }

    public void AddGraphQLType(Action<IObjectTypeDescriptor> objectTypeDescriptor) => _graphQLTypes.Add(objectTypeDescriptor);
}
