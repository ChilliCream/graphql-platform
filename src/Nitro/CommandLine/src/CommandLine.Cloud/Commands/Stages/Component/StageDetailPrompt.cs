using ChilliCream.Nitro.CommandLine.Cloud.Client;

namespace ChilliCream.Nitro.CommandLine.Cloud;

internal sealed class StageDetailPrompt
{
    private readonly IStageDetailPrompt_Stage _data;

    private StageDetailPrompt(IStageDetailPrompt_Stage data)
    {
        _data = data;
    }

    public object ToObject()
    {
        return new
        {
            _data.Id,
            _data.Name,
            Conditions = _data.Conditions
                .OfType<IAfterStageCondition>()
                .Select(x => new { Kind = "AfterStage", x.AfterStage!.Name })
        };
    }

    public static StageDetailPrompt From(IStageDetailPrompt_Stage data)
        => new(data);
}
