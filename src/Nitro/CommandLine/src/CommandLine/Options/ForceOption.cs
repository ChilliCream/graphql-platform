namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class ForceOption : Option<bool>
{
    public ForceOption() : base("--force")
    {
        Description = "Skip confirmation prompts for deletes and overwrites";
        Required = false;
    }
}
