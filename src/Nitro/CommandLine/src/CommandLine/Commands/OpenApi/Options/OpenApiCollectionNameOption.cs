using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.OpenApi.Options;

internal sealed class OpenApiCollectionNameOption : Option<string>
{
    public OpenApiCollectionNameOption() : base("--name")
    {
        Description = "The name of the OpenAPI collection";
        IsRequired = false;
        this.DefaultFromEnvironmentValue("OPENAPI_COLLECTION_NAME");
    }
}
