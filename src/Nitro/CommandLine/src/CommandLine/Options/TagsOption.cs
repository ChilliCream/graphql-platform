namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class TagsOption : Option<IEnumerable<string>>
{
    public TagsOption() : base("--tag")
    {
        Description = "The tag(s) of the schema version to deploy";
        Required = true;
        AllowMultipleArgumentsPerToken = true;
        this.DefaultFromEnvironmentValue("TAG");
    }
}
