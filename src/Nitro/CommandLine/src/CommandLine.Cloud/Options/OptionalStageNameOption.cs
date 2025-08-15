namespace ChilliCream.Nitro.CLI.Option;

internal sealed class OptionalStageNameOption : Option<string>
{
    public OptionalStageNameOption() : base("--stage")
    {
        Description = "The name of the stage";
        IsRequired = true;
        this.DefaultFromEnvironmentValue("STAGE");
    }
}
