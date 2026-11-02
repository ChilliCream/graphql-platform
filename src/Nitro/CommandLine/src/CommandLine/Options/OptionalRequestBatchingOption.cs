namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalRequestBatchingOption : Option<string>
{
    public const string OptionName = "--request-batching";

    public OptionalRequestBatchingOption() : base(OptionName)
    {
        Description = "Whether the source schema supports request batching. Defaults to false";
        AcceptOnlyFromAmong("true", "false");
    }
}
