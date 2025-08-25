using System.CommandLine.Invocation;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Cloud.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Helpers;
using ChilliCream.Nitro.CommandLine.Cloud.Option;
using ChilliCream.Nitro.CommandLine.Cloud.Option.Binders;
using ChilliCream.Nitro.CommandLine.Cloud.Results;
using static ChilliCream.Nitro.CommandLine.Cloud.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.Stages;

internal sealed class EditStagesCommand : Command
{
    private static readonly StageUpdateInput s_defaultStage = new()
    {
        Name = "default",
        DisplayName = "Default",
        Conditions = Array.Empty<StageConditionUpdateInput>()
    };

    public EditStagesCommand()
        : base("edit")
    {
        Description = "Edit stages of an API.";

        AddOption(Opt<OptionalApiIdOption>.Instance);
        AddOption(Opt<StageConfigurationOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Bind.FromServiceProvider<CancellationToken>()
        );
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        CancellationToken cancellationToken
    )
    {
        console.WriteOperationTitle();

        const string apiMessage = "For which api do you want to create a client?";

        var apiId = await context.GetOrSelectApiId(apiMessage);

        var stageConfiguration = context.ParseResult.GetValueForOption(
            Opt<StageConfigurationOption>.Instance
        );

        if (stageConfiguration is not null)
        {
            StageUpdateInput[]? input = null;
            try
            {
                input = JsonSerializer
                    .Deserialize(
                        stageConfiguration,
                        NitroCLIJsonContext.Default.StageConfigurationParameterArray
                    )
                    ?.Select(x => x.ToStageUpdateInput())
                    .ToArray();
            }
            catch
            {
                //  do nothing
            }

            if (input is null)
            {
                throw Exit("Could not parse stage configuration");
            }

            await client.UpdateStagesAsync(context, console, apiId, input, cancellationToken);
            return ExitCodes.Success;
        }

        return await EditStagesInteractivlyAsync(
            context,
            console,
            client,
            apiId,
            cancellationToken
        );
    }

    private static async Task<int> EditStagesInteractivlyAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        string apiId,
        CancellationToken cancellationToken
    )
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

                    await client.UpdateStagesAsync(
                        context,
                        console,
                        apiId,
                        updatedStages,
                        cancellationToken
                    );

                    return ExitCodes.Success;
            }
        }

        return ExitCodes.Error;
    }
}

file static class ClientExtensions
{
    public static async Task<IReadOnlyList<StageUpdateInput>> FetchStagesAsync(
        this IApiClient client,
        string apiId,
        CancellationToken cancellationToken
    )
    {
        var result = await client.ListStagesQuery.ExecuteAsync(apiId, cancellationToken);
        var data = result.EnsureData();
        var stages = (data.Node as IListStagesQuery_Node_Api)?.Stages;
        if (stages is null)
        {
            throw Exit("Could not load stages");
        }

        return stages
            .Select(x => new StageUpdateInput
            {
                Name = x.Name,
                DisplayName = x.DisplayName,
                Conditions = x
                    .Conditions.OfType<IAfterStageCondition>()
                    .Select(y => new StageConditionUpdateInput { AfterStage = y.AfterStage!.Name })
                    .ToArray()
            })
            .ToList();
    }

    public static async Task UpdateStagesAsync(
        this IApiClient client,
        InvocationContext context,
        IAnsiConsole console,
        string apiId,
        IReadOnlyList<StageUpdateInput> updatedStages,
        CancellationToken cancellationToken
    )
    {
        var updateInput = new UpdateStagesInput() { ApiId = apiId, UpdatedStages = updatedStages };

        var updateResult = await client.UpdateStages.ExecuteAsync(updateInput, cancellationToken);

        updateResult.EnsureData();
        console.PrintErrorsAndExit(updateResult.Data?.UpdateStages.Errors);

        var items = updateResult.Data?.UpdateStages.Api?.Stages
            .Select(x => StageDetailPrompt.From(x).ToObject())
            .ToArray() ?? [];

        context.SetResult(new PaginatedListResult<StageDetailPrompt.StageDetailPromptResult>(items, null));

        console.OkLine("Successfully updated stages");
    }
}

file static class Extensions
{
    public static async Task<ActionResult?> ShowStages(
        this IAnsiConsole console,
        string apiId,
        IReadOnlyList<StageUpdateInput> updatedStages,
        CancellationToken cancellationToken
    )
    {
        if (updatedStages.Count == 0)
        {
            return new ActionResult.Add();
        }

        ActionResult? result = null;

        await SelectableTable
            .From(updatedStages)
            .Title($"Edit the stages of api {apiId}")
            .AddColumn("Name", x => x.Name)
            .AddColumn("DisplayName", x => x.DisplayName)
            .AddColumn("After", x => x.Conditions!.Select(y => y.AfterStage).Join(","))
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

    public static async Task<StageUpdateInput> EditStage(
        this IAnsiConsole console,
        IEnumerable<StageUpdateInput> updatedStages,
        StageUpdateInput selectedStage,
        CancellationToken cancellationToken
    )
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

            foreach (var condition in selectedStage.Conditions!)
            {
                prompt.Select(condition.AfterStage);
            }

            selectedStages = await prompt.ShowAsync(console, cancellationToken);
        }
        else
        {
            selectedStages = Array.Empty<string>();
        }

        var conditionInputs = selectedStages
            .Select(x => new StageConditionUpdateInput() { AfterStage = x })
            .ToArray();

        return new StageUpdateInput
        {
            Name = name,
            DisplayName = displayName,
            Conditions = conditionInputs
        };
    }

    public static async Task<bool> ConfirmStageUpdate(
        this IAnsiConsole console,
        IReadOnlyList<StageUpdateInput> updatedStages,
        CancellationToken cancellationToken
    )
    {
        console.MarkupLine("Do you want to save the following changes?".AsQuestion());
        var resultTable = new Table().AddColumn("Name").AddColumn("DisplayName").AddColumn("After");

        foreach (var stage in updatedStages)
        {
            resultTable.AddRow(
                stage.Name,
                stage.DisplayName,
                stage.Conditions!.Select(x => x.AfterStage).Join(",")
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

    public static StageUpdateInput ToStageUpdateInput(this StageConfigurationParameter stage) =>
        new()
        {
            Name = stage.Name,
            DisplayName = stage.DisplayName,
            Conditions = stage
                .Conditions.Select(x => new StageConditionUpdateInput { AfterStage = x.AfterStage })
                .ToArray()
        };
}

file static class StageUpdateInputListExtensions
{
    public static IReadOnlyList<StageUpdateInput> Remove(
        this IReadOnlyList<StageUpdateInput> stages,
        StageUpdateInput toRemove
    )
    {
        return stages
            .Where(x => x != toRemove)
            .Select(x =>
                x with
                {
                    Conditions = x.Conditions!.Where(y => y.AfterStage != toRemove.Name).ToArray(),
                }
            )
            .ToArray();
    }

    public static IReadOnlyList<StageUpdateInput> Replace(
        this IReadOnlyList<StageUpdateInput> stages,
        StageUpdateInput toReplace,
        StageUpdateInput replacement
    ) => stages.Select(x => x == toReplace ? replacement : x).ToArray();

    public static IReadOnlyList<StageUpdateInput> Add(
        this IReadOnlyList<StageUpdateInput> stages,
        StageUpdateInput add
    ) => stages.Append(add).ToArray();
}

file static class SelectableTableExtensions
{
    public static SelectableTable<TEdge> AddItemKeyAction<TEdge>(
        this SelectableTable<TEdge> table,
        char key,
        Action<SelectableTable<TEdge>, int> action
    )
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
        Action<SelectableTable<TEdge>, int> action
    )
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

file static class ConsoleExtensions
{
    public static void WriteOperationTitle(this IAnsiConsole console)
    {
        console.WriteLine();
        console.WriteLine("Update stages");
        console.WriteLine();
    }
}

file record ActionResult
{
    public record Add : ActionResult;

    public record Edit(StageUpdateInput Stage) : ActionResult;

    public record Delete(StageUpdateInput Stage) : ActionResult;

    public record Save : ActionResult;
}
