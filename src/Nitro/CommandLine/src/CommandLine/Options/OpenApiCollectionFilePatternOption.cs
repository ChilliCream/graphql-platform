namespace ChilliCream.Nitro.CommandLine.Options;

public class OpenApiCollectionFilePatternOption: Option<List<string>>
{
    public OpenApiCollectionFilePatternOption() : base("--patterns")
    {
        Description = "TODO";
        IsRequired = true;

        AddAlias("-p");
    }
}
