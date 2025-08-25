using ChilliCream.Nitro.CommandLine.Cloud.Client;

namespace ChilliCream.Nitro.CommandLine.Cloud;

internal sealed class StageDetailPrompt
{
    private readonly IStageDetailPrompt_Stage _data;

    private StageDetailPrompt(IStageDetailPrompt_Stage data)
    {
        _data = data;
    }

    public StageDetailPromptResult ToObject()
    {
        return new StageDetailPromptResult
        {
            Id = _data.Id,
            Name = _data.Name,
            Conditions = _data.Conditions
                .OfType<IAfterStageCondition>()
                .Select(x => new StageCondition { Kind = "AfterStage", Name = x.AfterStage!.Name })
                .ToList()
        };
    }

    public static StageDetailPrompt From(IStageDetailPrompt_Stage data)
        => new(data);

    public class StageDetailPromptResult
    {
        public required string Id { get; init; }

        public required string Name { get; init; }

        public required IReadOnlyList<StageCondition> Conditions { get; init; }
    }

    public class StageCondition
    {
        public required string Kind { get; init; }

        public required string Name { get; init; }
    }
}
