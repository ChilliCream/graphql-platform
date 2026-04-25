using System.Collections.Immutable;
using HotChocolate.Adapters.OpenApi.Configuration;

namespace HotChocolate.Adapters.OpenApi;

internal interface IOpenApiProvider
{
    ImmutableArray<string> Names { get; }

    OpenApiSetup GetSetup(string? name = null);
}
