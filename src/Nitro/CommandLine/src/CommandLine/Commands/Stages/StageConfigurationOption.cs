namespace ChilliCream.Nitro.CommandLine.Commands.Stages;

internal sealed class StageConfigurationOption : Option<string>
{
    public StageConfigurationOption() : base("--configuration")
    {
        Description = "The stage configuration. If not provided, an interactive selection will be"
            + "shown. This input is a JSON array of stage configuration in the following format:"
            + """[{"name":"stage1","displayName":"Stage 1","conditions":[{"afterStage":"stage2"}]},...]""";
    }
}
