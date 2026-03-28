namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class OptionalApiKeyStageConditionOption : Option<string>
{
    public OptionalApiKeyStageConditionOption()
        : base("--stage-condition")
    {
        Description =
            "**PREVIEW** Limit the API key to a specific stage name. If not provided, the API key will be valid for all stages.";
        Required = false;
    }
}
