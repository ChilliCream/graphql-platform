using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.OpenApi.Options;

internal sealed class OpenApiCollectionIdOption : Option<string>
{
    public OpenApiCollectionIdOption() : base("--openapi-collection-id")
    {
        Description = "The ID of the OpenAPI collection";
        Required = true;
        this.DefaultFromEnvironmentValue("OPENAPI_COLLECTION_ID");
        this.NonEmptyStringsOnly();
    }
}
