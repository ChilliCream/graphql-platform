namespace ChilliCream.Nitro.CommandLine.Commands.OpenApi.Options;

internal sealed class OpenApiCollectionNameOption : Option<string>
{
    public OpenApiCollectionNameOption() : base("--name")
    {
        Description = "The name of the OpenAPI collection";
        Required = false;
        this.DefaultFromEnvironmentValue(EnvironmentVariables.OpenApiCollectionName);
    }
}
