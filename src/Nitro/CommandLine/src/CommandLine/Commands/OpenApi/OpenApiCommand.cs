namespace ChilliCream.Nitro.CommandLine.Commands.OpenApi;

internal sealed class OpenApiCommand : Command
{
    public OpenApiCommand() : base("openapi")
    {
        Description = "Manage OpenAPI collections";

        this.AddNitroCloudDefaultOptions();

        AddCommand(new CreateOpenApiCollectionCommand());
        AddCommand(new DeleteOpenApiCollectionCommand());
        AddCommand(new ListOpenApiCollectionCommand());
        AddCommand(new UploadOpenApiCollectionCommand());
        AddCommand(new PublishOpenApiCollectionCommand());
        AddCommand(new ValidateOpenApiCollectionCommand());
    }
}
