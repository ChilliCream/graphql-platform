namespace ChilliCream.Nitro.CommandLine.Cloud.Option;

public class OpenApiCollectionFilePatternOption: Option<List<string>>
{
    public OpenApiCollectionFilePatternOption() : base("--patterns")
    {
        Description = "TODO";
        IsRequired = true;

        AddAlias("-p");
    }
}
