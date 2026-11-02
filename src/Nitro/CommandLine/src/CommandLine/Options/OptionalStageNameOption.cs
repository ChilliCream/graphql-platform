namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalStageNameOption : Option<string>
{
    public OptionalStageNameOption() : base("--stage")
    {
        Description = "The name of the stage";
        Required = true;
        this.DefaultFromEnvironmentValue(EnvironmentVariables.Stage);
        this.NonEmptyStringsOnly();
    }
}
