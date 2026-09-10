namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalVariableBatchingOption : Option<string>
{
    public const string OptionName = "--variable-batching";

    public OptionalVariableBatchingOption() : base(OptionName)
    {
        Description = "Whether the source schema supports variable batching. Defaults to false";
        AcceptOnlyFromAmong("true", "false");
    }
}
