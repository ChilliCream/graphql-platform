using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks;

internal sealed class DoctorTaskCommand : Command
{
    public DoctorTaskCommand() : base("doctor")
    {
        Description = "Check the task workspace for integrity problems.";

        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent tasks doctor");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<ITaskStore>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var workspaceDirectory = store.FindWorkspaceDirectory()
            ?? throw new ExitException(
                "No agent workspace found. Run `nitro agent init` first.");

        var prefix = await store.GetPrefixAsync(cancellationToken);
        var taskCount = await store.CountTasksAsync(cancellationToken);
        var integrity = await store.CheckIntegrityAsync(cancellationToken);
        var cycles = await FindBlockingCyclesAsync(store, cancellationToken);
        var healthy = integrity.QuickCheckOk
            && cycles.Count == 0
            && integrity.OrphanDependencies.Count == 0
            && integrity.OrphanLabels.Count == 0
            && integrity.OrphanComments.Count == 0
            && integrity.TombstonedParentEdges.Count == 0;

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(new TaskDoctorResult(
                workspaceDirectory,
                prefix,
                taskCount,
                integrity.QuickCheckOk,
                integrity.QuickCheckMessage,
                cycles.Select(cycle => new TaskCycleResult(cycle)).ToArray(),
                integrity.OrphanDependencies,
                integrity.OrphanLabels,
                integrity.OrphanComments,
                integrity.TombstonedParentEdges,
                healthy)));

            return healthy ? ExitCodes.Success : ExitCodes.Error;
        }

        console.WriteLine($"Workspace: {workspaceDirectory}");
        console.WriteLine($"Prefix: {prefix}");
        console.WriteLine($"Tasks: {taskCount}");
        console.WriteLine();

        WriteCheck(
            console,
            "SQLite quick_check",
            integrity.QuickCheckOk,
            [integrity.QuickCheckMessage]);

        WriteCheck(
            console,
            "Dependency cycles",
            cycles.Count == 0,
            cycles.Select(cycle => string.Join(" -> ", cycle) + " -> " + cycle[0]));

        WriteCheck(
            console,
            "Orphan dependencies",
            integrity.OrphanDependencies.Count == 0,
            integrity.OrphanDependencies.Select(edge => $"{edge.TaskId} -> {edge.DependsOnId}"));

        WriteCheck(
            console,
            "Orphan labels",
            integrity.OrphanLabels.Count == 0,
            integrity.OrphanLabels.Select(label => $"{label.TaskId}: '{label.Label}'"));

        WriteCheck(
            console,
            "Orphan comments",
            integrity.OrphanComments.Count == 0,
            integrity.OrphanComments.Select(comment => $"{comment.TaskId}: comment #{comment.CommentId}"));

        WriteCheck(
            console,
            "Tombstoned parent edges",
            integrity.TombstonedParentEdges.Count == 0,
            integrity.TombstonedParentEdges.Select(edge => $"{edge.TaskId} -> {edge.DependsOnId}"));

        return healthy ? ExitCodes.Success : ExitCodes.Error;
    }

    private static void WriteCheck(
        INitroConsole console,
        string name,
        bool ok,
        IEnumerable<string> problems)
    {
        if (ok)
        {
            console.OkLine(name);
            return;
        }

        console.WriteLine($"FAIL {name}:");

        foreach (var problem in problems)
        {
            console.WriteLine($"  {problem}");
        }
    }

    private static async Task<List<List<string>>> FindBlockingCyclesAsync(
        ITaskStore store,
        CancellationToken cancellationToken)
    {
        var dependencyEdges = await store.GetDependencyEdgesAsync(cancellationToken);
        var edges = dependencyEdges.Where(edge => TaskDependencyTypes.IsBlocking(edge.Type)).ToList();

        var adjacency = edges
            .GroupBy(edge => edge.TaskId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(edge => edge.DependsOnId)
                    .Distinct()
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToList());

        return CyclesTaskDependencyCommand.FindCycles(adjacency);
    }

    public sealed record TaskDoctorResult(
        string WorkspacePath,
        string Prefix,
        int TaskCount,
        bool QuickCheckOk,
        string QuickCheckMessage,
        IReadOnlyList<TaskCycleResult> Cycles,
        IReadOnlyList<TaskDependencyReference> OrphanDependencies,
        IReadOnlyList<TaskOrphanLabel> OrphanLabels,
        IReadOnlyList<TaskOrphanComment> OrphanComments,
        IReadOnlyList<TaskDependencyReference> TombstonedParentEdges,
        bool Healthy);
}
