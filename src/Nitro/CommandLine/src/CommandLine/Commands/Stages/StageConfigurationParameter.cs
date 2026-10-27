namespace ChilliCream.Nitro.CommandLine.Commands.Stages;

internal sealed class StageConfigurationParameter
{
    public string Name { get; set; } = default!;

    public string DisplayName { get; set; } = default!;

    public IReadOnlyList<StageConditionParameter> Conditions { get; set; } =
        Array.Empty<StageConditionParameter>();
}
