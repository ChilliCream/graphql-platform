namespace ChilliCream.Nitro.CommandLine.Cloud.Option;

internal sealed class OptionalApiKeyStageConditionOption : Option<string>
{
    public OptionalApiKeyStageConditionOption()
        : base("--stage-condition")
    {
        Description =
            "**PREVIEW** Limit the api key to a specific stage name. If not provided, the api key will be valid for all stages.";
        IsRequired = false;
    }
}
