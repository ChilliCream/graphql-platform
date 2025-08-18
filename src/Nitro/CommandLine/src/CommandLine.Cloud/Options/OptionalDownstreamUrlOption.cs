namespace ChilliCream.Nitro.CommandLine.Cloud.Option;

internal sealed class OptionalDownstreamUrlOption : DownstreamUrlOption
{
    public OptionalDownstreamUrlOption() : base()
    {
        IsRequired = false;
    }
}
