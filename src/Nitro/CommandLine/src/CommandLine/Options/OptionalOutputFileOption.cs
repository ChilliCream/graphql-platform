namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalOutputFileOption : OutputFileOption
{
    public OptionalOutputFileOption()
    {
        Required = false;
    }
}
