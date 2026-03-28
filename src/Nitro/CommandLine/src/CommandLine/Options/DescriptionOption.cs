namespace ChilliCream.Nitro.CommandLine.Options;

internal class DescriptionOption : Option<string>
{
    public DescriptionOption() : base("--description")
    {
        Description = "The description of the pat";
        Required = false;
        this.DefaultFromEnvironmentValue("DESCRIPTION");
    }
}

internal sealed class OptionalDescriptionOption : DescriptionOption
{
    public OptionalDescriptionOption() : base()
    {
        Required = false;
    }
}
