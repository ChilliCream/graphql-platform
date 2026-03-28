namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class OptionalDownstreamUrlOption : DownstreamUrlOption
{
    public OptionalDownstreamUrlOption() : base()
    {
        Required = false;
    }
}
