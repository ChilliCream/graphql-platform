namespace ChilliCream.Nitro.CommandLine.Commands.OpenApi.Options;

internal sealed class OptionalOpenApiCollectionIdOption : OpenApiCollectionIdOption
{
    public OptionalOpenApiCollectionIdOption() : base()
    {
        Required = false;
    }
}
