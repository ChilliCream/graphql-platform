namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class StageNameOption : Option<string>
{
    public StageNameOption() : base("--stage")
    {
        Description = "The name of the stage";
        Required = true;
        this.DefaultFromEnvironmentValue("STAGE");
        this.NonEmptyStringsOnly();
    }
}
