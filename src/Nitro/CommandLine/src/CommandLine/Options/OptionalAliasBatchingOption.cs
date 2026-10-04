namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalAliasBatchingOption : Option<string>
{
    public const string OptionName = "--alias-batching";

    public OptionalAliasBatchingOption() : base(OptionName)
    {
        Description = "Whether the source schema supports alias batching. Defaults to true";
        AcceptOnlyFromAmong("true", "false");
    }
}
