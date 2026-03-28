namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class FusionRunPortOption : Option<int>
{
    public FusionRunPortOption() : base("--port")
    {
        AddAlias("-p");
    }
}
