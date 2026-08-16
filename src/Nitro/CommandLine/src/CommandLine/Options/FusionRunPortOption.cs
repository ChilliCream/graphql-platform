namespace ChilliCream.Nitro.CommandLine;

internal sealed class FusionRunPortOption : Option<int?>
{
    public FusionRunPortOption() : base("--port")
    {
        Description = "The port the router will listen on";
        Aliases.Add("-p");
    }
}
