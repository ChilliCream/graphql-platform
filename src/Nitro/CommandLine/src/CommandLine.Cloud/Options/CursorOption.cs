namespace ChilliCream.Nitro.CLI.Option;

internal sealed class CursorOption : Option<string?>
{
    public CursorOption() : base("--cursor")
    {
        Description = "The cursor to start the query (non interactive mode)";
        IsRequired = false;
        this.DefaultFromEnvironmentValue("CURSOR");
    }
}
