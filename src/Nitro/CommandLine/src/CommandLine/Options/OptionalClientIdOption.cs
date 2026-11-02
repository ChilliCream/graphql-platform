namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalClientIdOption : ClientIdOption
{
    public OptionalClientIdOption() : base()
    {
        Required = false;
    }
}
