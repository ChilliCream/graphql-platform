using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using Dapper;

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

        await using var connection = await store.ConnectAsync(cancellationToken);
        var task = await store.GetRequiredTaskAsync(connection, id, cancellationToken);

        var dependencies = (await connection.QueryAsync<DependencyRow>(
            """
            SELECT d.dependency_type AS Type, d.depends_on_id AS DependsOnId,
                   t.status AS Status, t.title AS Title
            FROM dependencies d
            LEFT JOIN tasks t ON t.id = d.depends_on_id
            WHERE d.task_id = @id
            ORDER BY d.created_at, d.depends_on_id
            """,
            new { id = task.Id })).ToList();

        var dependents = (await connection.QueryAsync<DependentRow>(
            """
            SELECT d.task_id AS TaskId, d.dependency_type AS Type,
                   t.status AS Status, t.title AS Title
            FROM dependencies d
            LEFT JOIN tasks t ON t.id = d.task_id
            WHERE d.depends_on_id = @id
            ORDER BY d.created_at, d.task_id
            """,
            new { id = task.Id })).ToList();

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

    private static string FormatDependency(DependencyRow dependency)
    {
        var status = dependency.Status ?? "unknown";
        var line = $"  {dependency.Type} -> {dependency.DependsOnId} ({status})";

        if (!string.IsNullOrEmpty(dependency.Title))
        {
            line += $" {dependency.Title}";
        }

        return line;
    }

    private static string FormatDependent(DependentRow dependent)
    {
        var status = dependent.Status ?? "unknown";
        var line = $"  {dependent.TaskId} ({dependent.Type}, {status})";

        if (!string.IsNullOrEmpty(dependent.Title))
        {
            line += $" {dependent.Title}";
        }

        return line;
    }

    private sealed class DependencyRow
    {
        public required string Type { get; init; }
        public required string DependsOnId { get; init; }
        public string? Status { get; init; }
        public string? Title { get; init; }
    }

    private sealed class DependentRow
    {
        public required string TaskId { get; init; }
        public required string Type { get; init; }
        public string? Status { get; init; }
        public string? Title { get; init; }
    }
}
