namespace ChilliCream.Nitro.CLI.Commands.Stages;

internal sealed class StageConfigurationOption : Option<string>
{
    public StageConfigurationOption() : base("--configuration")
    {
        Description = "The stage configuration. If not provided, an interactive selection will be" +
            "shown. This input is a JSON array of stage configuration in the following format:" +
            """[{"name":"stage1","displayName":"Stage 1","conditions":[{"afterStage":"stage2"}]},...]""";
    }
}

internal sealed class StageConfigurationParameter
{
    public string Name { get; set; } = default!;

    public string DisplayName { get; set; } = default!;

    public IReadOnlyList<StageConditionParameter> Conditions { get; set; } =
        Array.Empty<StageConditionParameter>();
}

internal sealed class StageConditionParameter
{
    public string AfterStage { get; set; } = default!;
}
