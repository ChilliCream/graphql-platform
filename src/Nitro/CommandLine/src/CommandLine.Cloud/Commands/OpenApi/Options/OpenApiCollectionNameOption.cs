using ChilliCream.Nitro.CommandLine.Cloud.Option;

namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.OpenApi.Options;

internal sealed class OpenApiCollectionNameOption : Option<string>
{
    public OpenApiCollectionNameOption() : base("--name")
    {
        Description = "The name of the OpenAPI collection";
        IsRequired = true;
        this.DefaultFromEnvironmentValue("OPENAPI_COLLECTION_NAME");
    }
}
