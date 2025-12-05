namespace ChilliCream.Nitro.CommandLine.Cloud.Option;

internal sealed class OpenApiCollectionIdOption : Option<string>
{
    public OpenApiCollectionIdOption() : base("--openapi-collection-id")
    {
        Description = "The id of the OpenAPI collection";
        IsRequired = true;
        this.DefaultFromEnvironmentValue("OPENAPI_COLLECTION_ID");
    }
}
