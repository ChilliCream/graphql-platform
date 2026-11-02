namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalForceOption : Option<bool>
{
    public OptionalForceOption() : base("--force")
    {
        Description = "Skip confirmation prompts for deletes and overwrites";
        Required = false;
    }
}
