namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class OptionalApiIdOption : ApiIdOption
{
    public OptionalApiIdOption() : base()
    {
        Required = false;
    }
}
