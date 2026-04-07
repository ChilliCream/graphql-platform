namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalApiKeyStageConditionOption : Option<string>
{
    public OptionalApiKeyStageConditionOption()
        : base("--stage-condition")
    {
        Description =
            "[Preview] Limit the API key to a specific stage name (if not set, the key is valid for all stages)";
        Required = false;
    }
}
