namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalForceOption : Option<bool>
{
    public const string OptionName = "--force";

    public OptionalForceOption() : base(OptionName)
    {
        Description = "Skip confirmation prompts for deletes and overwrites";
        Required = false;
    }
}
