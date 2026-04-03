namespace ChilliCream.Nitro.CommandLine;

internal sealed class StageNameOption : Option<string>
{
    public StageNameOption() : base("--stage")
    {
        Description = "The name of the stage";
        Required = true;
        this.DefaultFromEnvironmentValue(EnvironmentVariables.Stage);
        this.NonEmptyStringsOnly();
    }
}
