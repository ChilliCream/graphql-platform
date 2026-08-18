using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class AddTaskLabelCommand : Command
{
    public AddTaskLabelCommand() : base("add")
    {
        Description = "Add one or more labels to a task.";

        Arguments.Add(Opt<TaskIdArgument>.Instance);
        Arguments.Add(Opt<LabelsArgument>.Instance);
        Options.Add(Opt<TaskActorOption>.Instance);

        this.AddExamples("task label add \"acme-1a2\" api parser");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<ITaskStore>();
        var environmentVariableProvider = services.GetRequiredService<IEnvironmentVariableProvider>();

        var id = parseResult.GetRequiredValue(Opt<TaskIdArgument>.Instance);
        var labels = parseResult.GetRequiredValue(Opt<LabelsArgument>.Instance);
        var actor = TaskActor.Resolve(
            parseResult.GetValue(Opt<TaskActorOption>.Instance), environmentVariableProvider);

        var results = await store.AddLabelAsync(id, labels, actor, cancellationToken);

        foreach (var result in results)
        {
            if (result.Added)
            {
                console.OkLine($"Added label '{result.Label.EscapeMarkup()}' to '{id.EscapeMarkup()}'.");
            }
            else
            {
                console.WriteLine($"Label '{result.Label}' is already on '{id}'.");
            }
        }

        return ExitCodes.Success;
    }
}
