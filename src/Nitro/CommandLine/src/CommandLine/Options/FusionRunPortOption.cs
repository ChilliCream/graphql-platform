namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class FusionRunPortOption : Option<int?>
{
    public FusionRunPortOption() : base("--port")
    {
        Description = "The port the gateway will listen on";
        Aliases.Add("-p");
    }
}
