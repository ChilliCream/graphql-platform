using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class ListTaskDependencyCommand : Command
{
    public ListTaskDependencyCommand() : base("list")
    {
        Description = "List a task's dependencies.";

        Arguments.Add(Opt<TaskIdArgument>.Instance);

        this.AddExamples("task dep list \"acme-1a2\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<ITaskStore>();

        var id = parseResult.GetRequiredValue(Opt<TaskIdArgument>.Instance);

        var task = await store.GetRequiredTaskAsync(id, cancellationToken);
        var dependencies = await store.GetDependenciesAsync(task.Id, cancellationToken);
        var dependents = await store.GetDependentsAsync(task.Id, cancellationToken);

        if (dependencies.Count == 0 && dependents.Count == 0)
        {
            console.WriteLine("No dependencies.");
            return ExitCodes.Success;
        }

        if (dependencies.Count > 0)
        {
            console.WriteLine($"Dependencies of {task.Id}:");

            foreach (var dependency in dependencies)
            {
                console.WriteLine(FormatDependency(dependency));
            }
        }

        if (dependents.Count > 0)
        {
            console.WriteLine("Depended on by:");

            foreach (var dependent in dependents)
            {
                console.WriteLine(FormatDependent(dependent));
            }
        }

        return ExitCodes.Success;
    }

    private static string FormatDependency(TaskDependencyDetail dependency)
    {
        var status = dependency.Status ?? "unknown";
        var line = $"  {dependency.Type} -> {dependency.DependsOnId} ({status})";

        if (!string.IsNullOrEmpty(dependency.Title))
        {
            line += $" {dependency.Title}";
        }

        return line;
    }

    private static string FormatDependent(TaskDependentDetail dependent)
    {
        var status = dependent.Status ?? "unknown";
        var line = $"  {dependent.TaskId} ({dependent.Type}, {status})";

        if (!string.IsNullOrEmpty(dependent.Title))
        {
            line += $" {dependent.Title}";
        }

        return line;
    }
}
