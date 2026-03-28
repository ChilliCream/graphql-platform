namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class OptionalCursorOption : Option<string?>
{
    public OptionalCursorOption() : base("--cursor")
    {
        Description = "The cursor to start the query (non interactive mode)";
        Required = false;
        this.DefaultFromEnvironmentValue("CURSOR");
    }
}
