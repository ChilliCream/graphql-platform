namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class OptionalClientIdOption : ClientIdOption
{
    public OptionalClientIdOption() : base()
    {
        Required = false;
    }
}
