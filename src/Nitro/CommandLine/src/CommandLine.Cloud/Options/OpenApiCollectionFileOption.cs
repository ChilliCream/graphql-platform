namespace ChilliCream.Nitro.CommandLine.Cloud.Option;

internal sealed class OpenApiCollectionFileOption : Option<FileInfo>
{
    public OpenApiCollectionFileOption() : base("--openapi-collection-file")
    {
        Description = "The path to an OpenAPI collection archive";
        IsRequired = true;
        this.DefaultFileFromEnvironmentValue("OPENAPI_COLLECTION_FILE");
    }
}
