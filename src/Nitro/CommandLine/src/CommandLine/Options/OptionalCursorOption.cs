namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalCursorOption : Option<string?>
{
    public OptionalCursorOption() : base("--cursor")
    {
        Description = "The pagination cursor to resume from";
        Required = false;
        this.DefaultFromEnvironmentValue(EnvironmentVariables.Cursor);
    }
}
