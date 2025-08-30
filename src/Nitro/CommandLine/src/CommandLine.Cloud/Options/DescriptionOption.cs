namespace ChilliCream.Nitro.CommandLine.Cloud.Option;

internal class DescriptionOption : Option<string>
{
    public DescriptionOption() : base("--description", "The description of the pat")
    {
        IsRequired = false;
        this.DefaultFromEnvironmentValue("DESCRIPTION");
    }
}

internal sealed class OptionalDescriptionOption : DescriptionOption
{
    public OptionalDescriptionOption() : base()
    {
        IsRequired = false;
    }
}
