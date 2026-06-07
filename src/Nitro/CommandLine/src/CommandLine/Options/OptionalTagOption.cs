namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalTagOption : Option<string>
{
    public OptionalTagOption() : base("--tag")
    {
        Description = "The tag of the schema version to deploy";
        Required = false;
        this.DefaultFromEnvironmentValue(EnvironmentVariables.Tag);
        this.NonEmptyStringsOnly();
    }
}
