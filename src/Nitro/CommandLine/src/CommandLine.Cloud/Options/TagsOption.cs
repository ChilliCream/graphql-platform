namespace ChilliCream.Nitro.CommandLine.Cloud.Option;

internal sealed class TagsOption : Option<IEnumerable<string>>
{
    public TagsOption() : base("--tag")
    {
        Description = "The tag(s) of the schema version to deploy";
        IsRequired = true;
        AllowMultipleArgumentsPerToken = true;
        this.DefaultFromEnvironmentValue("TAG");
    }
}
