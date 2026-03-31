namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class TagsOption : Option<IEnumerable<string>>
{
    public TagsOption() : base("--tag")
    {
        Description = "One or more tags of the schema versions to deploy.";
        Required = true;
        AllowMultipleArgumentsPerToken = true;
        this.DefaultFromEnvironmentValue("TAG");
    }
}
