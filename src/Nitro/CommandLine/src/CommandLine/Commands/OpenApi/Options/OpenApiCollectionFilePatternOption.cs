namespace ChilliCream.Nitro.CommandLine.Commands.OpenApi.Options;

public class OpenApiCollectionFilePatternOption: Option<List<string>>
{
    public OpenApiCollectionFilePatternOption() : base("--pattern")
    {
        Description = "One or more glob patterns for selecting OpenAPI document files";
        IsRequired = true;

        AddAlias("-p");
    }
}
