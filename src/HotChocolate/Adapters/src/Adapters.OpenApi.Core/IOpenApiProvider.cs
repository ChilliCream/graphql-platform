using System.Collections.Immutable;
using HotChocolate.Adapters.OpenApi.Configuration;

namespace HotChocolate.Adapters.OpenApi;

internal interface IOpenApiProvider
{
    ImmutableArray<string> SchemaNames { get; }

    OpenApiSetup GetSetup(string? schemaName = null);
}
