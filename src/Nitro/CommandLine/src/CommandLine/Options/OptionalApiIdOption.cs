namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalApiIdOption : ApiIdOption
{
    public OptionalApiIdOption() : base()
    {
        Required = false;
    }
}
