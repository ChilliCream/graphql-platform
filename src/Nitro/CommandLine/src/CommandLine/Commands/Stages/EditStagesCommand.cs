using System.Text.Json;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Stages;
using ChilliCream.Nitro.CommandLine.Commands.Stages.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Configuration;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Stages;

internal sealed class EditStagesCommand : Command
{
    private static readonly StageUpdateModel s_defaultStage = new(
        Name: "default",
        DisplayName: "Default",
        AfterStages: []);

    public EditStagesCommand() : base("edit")
    {
        Description = "Edit stages of an API.";

        Options.Add(Opt<OptionalApiIdOption>.Instance);
        Options.Add(Opt<StageConfigurationOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples(
            """
            stage edit \
              --configuration "[{\"name\":\"dev\",\"displayName\":\"Dev\",\"conditions\":[]}]" \
              --api-id "<api-id>"
            """);

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<IStagesClient>();
        var apisClient = services.GetRequiredService<IApisClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        parseResult.AssertHasAuthentication(sessionService);

        const string apiMessage = "For which API do you want to edit the stages?";

        var apiId = await parseResult.GetOrPromptForApiIdAsync(
            apiMessage,
            console,
            apisClient,
            sessionService,
            cancellationToken);

        var stageConfiguration = parseResult.GetValue(
            Opt<StageConfigurationOption>.Instance
        );

        if (stageConfiguration is not null)
        {
            StageUpdateModel[]? input = null;
            try
            {
                input = JsonSerializer
                    .Deserialize(
                        stageConfiguration,
                        NitroCLIJsonContext.Default.StageConfigurationParameterArray
                    )
                    ?.Select(x => x.ToStageUpdateModel())
                    .ToArray();
            }
            catch
            {
                // do nothing
            }

            if (input is null)
            {
                throw Exit("Could not parse stage configuration");
            }

            return await client.UpdateStagesAsync(console, resultHolder, apiId, input, cancellationToken);
        }

        return await EditStagesInteractivlyAsync(
            console,
            client,
            resultHolder,
            apiId,
            cancellationToken
        );
    }

    private static async Task<int> EditStagesInteractivlyAsync(
        INitroConsole console,
        IStagesClient client,
        IResultHolder resultHolder,
        string apiId,
        CancellationToken cancellationToken)
    {
        var updatedStages = await client.FetchStagesAsync(apiId, cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            console.Clear();

            var action = await console.ShowStages(apiId, updatedStages, cancellationToken);

            switch (action)
            {
                case ActionResult.Delete { Stage: var deletedStage }:
                    updatedStages = updatedStages.Remove(deletedStage);
                    break;

                case ActionResult.Add:
                    console.WriteLine("Add new stage: ");

                    updatedStages = updatedStages.Add(
                        await console.EditStage(updatedStages, s_defaultStage, cancellationToken)
                    );

                    break;

                case ActionResult.Edit { Stage: var stageToEdit }:
                    console.WriteLine(
                        $"Edit stage {stageToEdit.DisplayName} ({stageToEdit.Name}): "
                    );

                    updatedStages = updatedStages.Replace(
                        stageToEdit,
                        await console.EditStage(updatedStages, stageToEdit, cancellationToken)
                    );

                    break;

                case ActionResult.Save
                    when await console.ConfirmStageUpdate(updatedStages, cancellationToken):

                    return await client.UpdateStagesAsync(
                        console,
                        resultHolder,
                        apiId,
                        updatedStages,
                        cancellationToken
                    );
            }
        }

        return ExitCodes.Error;
    }
}

file static class ClientExtensions
{
    public static async Task<IReadOnlyList<StageUpdateModel>> FetchStagesAsync(
        this IStagesClient client,
        string apiId,
        CancellationToken cancellationToken)
    {
        var stages = await client.ListStagesAsync(apiId, cancellationToken) ?? [];

        return stages
            .Select(x => new StageUpdateModel(
                x.Name,
                x.DisplayName,
                x.Conditions
                    .OfType<IAfterStageCondition>()
                    .Select(y => y.AfterStage?.Name)
                    .OfType<string>()
                    .ToArray()))
            .ToList();
    }

    public static async Task<int> UpdateStagesAsync(
        this IStagesClient client,
        INitroConsole console,
        IResultHolder resultHolder,
        string apiId,
        IReadOnlyList<StageUpdateModel> updatedStages,
        CancellationToken cancellationToken)
    {
        await using (var activity = console.StartActivity(
            $"Updating stages for API '{apiId.EscapeMarkup()}'",
            "Failed to update the stages."))
        {
            var data = await client.UpdateStagesAsync(apiId, updatedStages, cancellationToken);

            if (data.Errors?.Count > 0)
            {
                activity.Fail();

                foreach (var error in data.Errors)
                {
                    var errorMessage = error switch
                    {
                        IApiNotFoundError err => err.Message,
                        IStageNotFoundError err => err.Message,
                        IStagesHavePublishedDependenciesError err => err.Message,
                        IStageValidationError err => err.Message,
                        IError err => "Unexpected mutation error: " + err.Message,
                        _ => "Unexpected mutation error."
                    };

                    console.Error.WriteErrorLine(errorMessage);
                }

                return ExitCodes.Error;
            }

            activity.Success($"Updated stages for API '{apiId.EscapeMarkup()}'.");

            var items = data.Api?.Stages
                .Select(x => StageDetailPrompt.From(x).ToObject())
                .ToArray() ?? [];

            resultHolder.SetResult(
                new ObjectResult(new PaginatedListResult<StageDetailPrompt.StageDetailPromptResult>(items, null)));

            return ExitCodes.Success;
        }
    }
}

file static class Extensions
{
    public static async Task<ActionResult?> ShowStages(
        this INitroConsole console,
        string apiId,
        IReadOnlyList<StageUpdateModel> updatedStages,
        CancellationToken cancellationToken)
    {
        if (updatedStages.Count == 0)
        {
            return new ActionResult.Add();
        }

        ActionResult? result = null;

        await SelectableTable
            .From(updatedStages)
            .Title($"Edit the stages of API {apiId}")
            .AddColumn("Name", x => x.Name)
            .AddColumn("DisplayName", x => x.DisplayName)
            .AddColumn("After", x => x.AfterStages.Join(","))
            .AddKeyAction(ConsoleKey.Enter, (_, _) => new InputAction.None())
            .AddItemKeyAction('e', (d, index) => result = new ActionResult.Edit(d.Items[index]))
            .AddItemKeyAction('d', (d, index) => result = new ActionResult.Delete(d.Items[index]))
            .AddStoppingKeyAction('a', (_, _) => result = new ActionResult.Add())
            .AddStoppingKeyAction('s', (_, _) => result = new ActionResult.Save())
            .AddSelectableAddon("(a)dd new stage", () => result = new ActionResult.Add())
            .AddSelectableAddon("(s)ave changes", () => result = new ActionResult.Save())
            .AddFooterAddon("press (e) to edit / press (d) to delete")
            .RenderAsync(console, cancellationToken);

        return result;
    }

    public static async Task<StageUpdateModel> EditStage(
        this INitroConsole console,
        IEnumerable<StageUpdateModel> updatedStages,
        StageUpdateModel selectedStage,
        CancellationToken cancellationToken)
    {
        var name = await console.AskAsync("Name", selectedStage.Name, cancellationToken);
        var displayName = await console.AskAsync(
            "Display Name",
            selectedStage.DisplayName,
            cancellationToken
        );

        IReadOnlyList<string> selectedStages;

        var conditions = updatedStages.Where(x => x != selectedStage).Select(x => x.Name).ToArray();

        if (conditions.Length > 0)
        {
            var prompt = new MultiSelectionPrompt<string>()
                .AddChoices(conditions)
                .Title($"Select the stages before {selectedStage.Name}".AsQuestion())
                .NotRequired();

            foreach (var condition in selectedStage.AfterStages)
            {
                prompt.Select(condition);
            }

            selectedStages = await prompt.ShowAsync(console, cancellationToken);
        }
        else
        {
            selectedStages = [];
        }

        return new StageUpdateModel(
            Name: name,
            DisplayName: displayName,
            AfterStages: selectedStages);
    }

    public static async Task<bool> ConfirmStageUpdate(
        this INitroConsole console,
        IReadOnlyList<StageUpdateModel> updatedStages,
        CancellationToken cancellationToken)
    {
        console.MarkupLine("Do you want to save the following changes?".AsQuestion());
        var resultTable = new Table().AddColumn("Name").AddColumn("DisplayName").AddColumn("After");

        foreach (var stage in updatedStages)
        {
            resultTable.AddRow(
                stage.Name,
                stage.DisplayName,
                stage.AfterStages.Join(",")
            );
        }

        console.Write(new Padder(resultTable, new Padding(2, 0, 0, 0)));

        return await new ConfirmationPrompt("[default]  Save changes[/]").ShowAsync(
            console,
            cancellationToken
        );
    }
}

file static class StringExtensions
{
    public static string Join(this IEnumerable<string> source, string separator) =>
        string.Join(separator, source);

    public static StageUpdateModel ToStageUpdateModel(this StageConfigurationParameter stage) =>
        new(
            Name: stage.Name,
            DisplayName: stage.DisplayName,
            AfterStages: stage.Conditions.Select(x => x.AfterStage).ToArray());
}

file static class StageUpdateModelListExtensions
{
    public static IReadOnlyList<StageUpdateModel> Remove(
        this IReadOnlyList<StageUpdateModel> stages,
        StageUpdateModel toRemove)
    {
        return stages
            .Where(x => x != toRemove)
            .Select(x => x with { AfterStages = x.AfterStages.Where(y => y != toRemove.Name).ToArray() })
            .ToArray();
    }

    public static IReadOnlyList<StageUpdateModel> Replace(
        this IReadOnlyList<StageUpdateModel> stages,
        StageUpdateModel toReplace,
        StageUpdateModel replacement
    ) => stages.Select(x => x == toReplace ? replacement : x).ToArray();

    public static IReadOnlyList<StageUpdateModel> Add(
        this IReadOnlyList<StageUpdateModel> stages,
        StageUpdateModel add
    ) => stages.Append(add).ToArray();
}

file static class SelectableTableExtensions
{
    public static SelectableTable<TEdge> AddItemKeyAction<TEdge>(
        this SelectableTable<TEdge> table,
        char key,
        Action<SelectableTable<TEdge>, int> action)
    {
        return table.AddKeyAction(
            key,
            (d, index) =>
            {
                if (index < d.Items.Count)
                {
                    action(d, index);
                    return new InputAction.Break();
                }

                return new InputAction.None();
            }
        );
    }

    public static SelectableTable<TEdge> AddStoppingKeyAction<TEdge>(
        this SelectableTable<TEdge> table,
        char key,
        Action<SelectableTable<TEdge>, int> action)
    {
        return table.AddKeyAction(
            key,
            (d, index) =>
            {
                action(d, index);
                return new InputAction.Break();
            }
        );
    }
}

file record ActionResult
{
    public record Add : ActionResult;

    public record Edit(StageUpdateModel Stage) : ActionResult;

    public record Delete(StageUpdateModel Stage) : ActionResult;

    public record Save : ActionResult;
}
