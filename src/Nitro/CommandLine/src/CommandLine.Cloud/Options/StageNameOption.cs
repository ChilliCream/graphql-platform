namespace ChilliCream.Nitro.CommandLine.Cloud.Option;

internal sealed class StageNameOption : Option<string>
{
    public StageNameOption() : base("--stage")
    {
        Description = "The name of the stage";
        IsRequired = true;
        this.DefaultFromEnvironmentValue("STAGE");
    }
}
