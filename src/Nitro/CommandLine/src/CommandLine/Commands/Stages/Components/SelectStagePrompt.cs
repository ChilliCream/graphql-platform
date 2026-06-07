using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Stages;

namespace ChilliCream.Nitro.CommandLine.Commands.Stages.Components;

internal sealed class SelectStagePrompt(IStagesClient client, string apiId)
{
    private string _title = "Select a stage from the list below.";

    public SelectStagePrompt Title(string title)
    {
        _title = title;
        return this;
    }

    public async Task<IListStagesQuery_Node_Stages?> RenderAsync(
        INitroConsole console,
        CancellationToken cancellationToken)
    {
        var stages = await client.ListStagesAsync(apiId, cancellationToken) ?? [];

        if (stages.Count == 0)
        {
            return null;
        }

        return await console.PromptAsync(
            _title,
            stages.ToArray(),
            static s => s.Name,
            cancellationToken);
    }

    public static SelectStagePrompt New(IStagesClient client, string apiId)
        => new(client, apiId);
}
