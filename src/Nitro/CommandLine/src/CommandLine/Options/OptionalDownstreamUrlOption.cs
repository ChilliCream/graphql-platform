namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalDownstreamUrlOption : DownstreamUrlOption
{
    public OptionalDownstreamUrlOption() : base()
    {
        Required = false;
    }
}
