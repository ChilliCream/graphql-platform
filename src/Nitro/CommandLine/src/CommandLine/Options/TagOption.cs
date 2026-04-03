namespace ChilliCream.Nitro.CommandLine;

internal sealed class TagOption : Option<string>
{
    public const string OptionName = "--tag";

    public TagOption() : base(OptionName)
    {
        Description = "The tag of the schema version to deploy";
        Required = true;
        this.DefaultFromEnvironmentValue(EnvironmentVariables.Tag);
        this.NonEmptyStringsOnly();
    }
}
