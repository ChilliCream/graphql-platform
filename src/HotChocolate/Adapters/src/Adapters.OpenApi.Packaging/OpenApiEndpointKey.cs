namespace HotChocolate.Adapters.OpenApi.Packaging;

public readonly record struct OpenApiEndpointKey(string HttpMethod, string Route);
