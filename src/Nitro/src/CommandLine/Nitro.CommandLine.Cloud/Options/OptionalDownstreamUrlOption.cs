namespace ChilliCream.Nitro.CLI.Option;

internal sealed class OptionalDownstreamUrlOption : DownstreamUrlOption
{
    public OptionalDownstreamUrlOption() : base()
    {
        IsRequired = false;
    }
}
