namespace ChilliCream.Nitro.CommandLine.Cloud.Option;

internal sealed class TagOption : Option<string>
{
    public TagOption() : base("--tag")
    {
        Description = "The tag of the schema version to deploy";
        IsRequired = true;
        this.DefaultFromEnvironmentValue("TAG");
    }
}
