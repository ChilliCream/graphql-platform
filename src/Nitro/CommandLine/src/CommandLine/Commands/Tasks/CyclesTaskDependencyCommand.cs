using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class CyclesTaskDependencyCommand : Command
{
    public CyclesTaskDependencyCommand() : base("cycles")
    {
        Description = "Detect cycles among blocking dependencies.";

        this.AddExamples("task dep cycles");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<ITaskStore>();

        var edges = (await store.GetDependencyEdgesAsync(cancellationToken))
            .Where(e => TaskDependencyTypes.IsBlocking(e.Type))
            .ToList();

        var adjacency = edges
            .GroupBy(e => e.TaskId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.DependsOnId)
                    .Distinct()
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToList());

        var cycles = FindCycles(adjacency);

        if (cycles.Count == 0)
        {
            console.WriteLine("No cycles found.");
            return ExitCodes.Success;
        }

        foreach (var cycle in cycles)
        {
            console.WriteLine(FormatCycle(cycle));
        }

        return ExitCodes.Success;
    }

    // Enumerates each elementary cycle exactly once: a search that starts at
    // a given node only continues through nodes ordinally greater than or
    // equal to it, so a cycle is only ever found while starting from its
    // smallest member.
    private static List<List<string>> FindCycles(IReadOnlyDictionary<string, List<string>> adjacency)
    {
        var cycles = new List<List<string>>();

        foreach (var start in adjacency.Keys.OrderBy(x => x, StringComparer.Ordinal))
        {
            var path = new List<string> { start };
            var onPath = new HashSet<string> { start };

            Walk(start, start, adjacency, path, onPath, cycles);
        }

        return cycles;
    }

    private static void Walk(
        string start,
        string current,
        IReadOnlyDictionary<string, List<string>> adjacency,
        List<string> path,
        HashSet<string> onPath,
        List<List<string>> cycles)
    {
        if (!adjacency.TryGetValue(current, out var neighbors))
        {
            return;
        }

        foreach (var next in neighbors)
        {
            if (next == start)
            {
                cycles.Add([.. path]);
                continue;
            }

            if (string.CompareOrdinal(next, start) < 0 || onPath.Contains(next))
            {
                continue;
            }

            path.Add(next);
            onPath.Add(next);

            Walk(start, next, adjacency, path, onPath, cycles);

            path.RemoveAt(path.Count - 1);
            onPath.Remove(next);
        }
    }

    private static string FormatCycle(IReadOnlyList<string> cycle)
        => string.Join(" -> ", cycle) + " -> " + cycle[0];
}
