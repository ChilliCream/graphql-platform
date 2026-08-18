using System.Globalization;
using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using Dapper;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class ShowTaskCommand : Command
{
    public ShowTaskCommand() : base("show")
    {
        Description = "Show a task's details.";

        Arguments.Add(Opt<TaskIdArgument>.Instance);

        this.AddExamples("task show \"acme-1a2\"");

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

        var labels = (await connection.QueryAsync<string>(
            "SELECT label FROM labels WHERE task_id = @id ORDER BY label",
            new { id = task.Id, cancellationToken })).ToList();

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

        var blocks = (await connection.QueryAsync<BlockRow>(
            """
            SELECT d.task_id AS TaskId, t.status AS Status, t.title AS Title
            FROM dependencies d
            LEFT JOIN tasks t ON t.id = d.task_id
            WHERE d.depends_on_id = @id
            ORDER BY d.created_at, d.task_id
            """,
            new { id = task.Id })).ToList();

        // The intercepted read path cannot convert the TEXT-stored timestamp
        // column to DateTimeOffset, so this materializes an all-primitives
        // row and parses the timestamp itself.
        var comments = (await connection.QueryAsync<TaskCommentRow>(
                $"SELECT {TaskComment.Columns} FROM comments WHERE task_id = @id "
                + "ORDER BY created_at, id",
                new { id = task.Id }))
            .Select(r => r.ToTaskComment())
            .ToList();

        var blocked = await store.ComputeBlockedAsync(connection, cancellationToken);

        console.WriteLine($"{task.Id}: {task.Title}");
        console.WriteLine();

        var separatorPending = false;

        WriteSection(console, BuildHeader(task, labels, blocked), ref separatorPending);
        WriteSection(console, BuildTextBlock("Description:", task.Description), ref separatorPending);
        WriteSection(console, BuildTextBlock("Design:", task.Design), ref separatorPending);
        WriteSection(
            console,
            BuildTextBlock("Acceptance criteria:", task.AcceptanceCriteria),
            ref separatorPending);
        WriteSection(console, BuildTextBlock("Notes:", task.Notes), ref separatorPending);
        WriteSection(console, BuildDependencies(dependencies, blocks), ref separatorPending);
        WriteSection(console, BuildComments(comments), ref separatorPending);

        return ExitCodes.Success;
    }

    private static List<string> BuildHeader(
        TaskItem task,
        IReadOnlyList<string> labels,
        IReadOnlyDictionary<string, IReadOnlyList<string>> blocked)
    {
        var lines = new List<string>
        {
            $"Status: {task.Status}  Priority: {TaskPriorities.Format(task.Priority)}  Type: {task.Type}"
        };

        if (!string.IsNullOrEmpty(task.Assignee))
        {
            lines.Add($"Assignee: {task.Assignee}");
        }

        if (task.DueAt is { } dueAt)
        {
            lines.Add($"Due: {TaskDates.Format(dueAt)}");
        }

        if (task.DeferUntil is { } deferUntil)
        {
            lines.Add($"Deferred until: {TaskDates.Format(deferUntil)}");
        }

        if (task.EstimatedMinutes is { } estimatedMinutes)
        {
            lines.Add($"Estimate: {estimatedMinutes}m");
        }

        lines.Add($"Created: {TaskDates.Format(task.CreatedAt)} by {task.CreatedBy}");
        lines.Add($"Updated: {TaskDates.Format(task.UpdatedAt)}");

        if (task.Status == TaskStates.Closed && task.ClosedAt is { } closedAt)
        {
            var closedLine = $"Closed: {TaskDates.Format(closedAt)}";

            if (!string.IsNullOrEmpty(task.CloseReason))
            {
                closedLine += $" ({task.CloseReason})";
            }

            lines.Add(closedLine);
        }

        if (labels.Count > 0)
        {
            lines.Add($"Labels: {string.Join(", ", labels)}");
        }

        if (blocked.TryGetValue(task.Id, out var blockers) && blockers.Count > 0)
        {
            lines.Add($"Blocked by: {string.Join(", ", blockers)}");
        }

        return lines;
    }

    private static List<string> BuildTextBlock(string header, string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        var lines = new List<string> { header };

        foreach (var line in text.ReplaceLineEndings("\n").Split('\n'))
        {
            lines.Add("  " + line);
        }

        return lines;
    }

    private static List<string> BuildDependencies(
        IReadOnlyList<DependencyRow> dependencies,
        IReadOnlyList<BlockRow> blocks)
    {
        var lines = new List<string>();

        if (dependencies.Count > 0)
        {
            lines.Add("Dependencies:");

            foreach (var dependency in dependencies)
            {
                var status = dependency.Status ?? "unknown";
                var line = $"  {dependency.Type} -> {dependency.DependsOnId} ({status})";

                if (!string.IsNullOrEmpty(dependency.Title))
                {
                    line += $" {dependency.Title}";
                }

                lines.Add(line);
            }
        }

        if (blocks.Count > 0)
        {
            lines.Add("Blocks:");

            foreach (var block in blocks)
            {
                var status = block.Status ?? "unknown";
                var line = $"  {block.TaskId} ({status})";

                if (!string.IsNullOrEmpty(block.Title))
                {
                    line += $" {block.Title}";
                }

                lines.Add(line);
            }
        }

        return lines;
    }

    private static List<string> BuildComments(IReadOnlyList<TaskComment> comments)
    {
        var lines = new List<string>();

        if (comments.Count == 0)
        {
            return lines;
        }

        lines.Add("Comments:");

        for (var i = 0; i < comments.Count; i++)
        {
            if (i > 0)
            {
                lines.Add(string.Empty);
            }

            var comment = comments[i];

            lines.Add($"  [{i + 1}] {comment.Author} {TaskDates.Format(comment.CreatedAt)}");

            foreach (var line in comment.Text.ReplaceLineEndings("\n").Split('\n'))
            {
                lines.Add("    " + line);
            }
        }

        return lines;
    }

    private static void WriteSection(
        INitroConsole console,
        IReadOnlyList<string> lines,
        ref bool separatorPending)
    {
        if (lines.Count == 0)
        {
            return;
        }

        if (separatorPending)
        {
            console.WriteLine();
        }

        foreach (var line in lines)
        {
            console.WriteLine(line);
        }

        separatorPending = true;
    }

    private sealed class DependencyRow
    {
        public required string Type { get; init; }
        public required string DependsOnId { get; init; }
        public string? Status { get; init; }
        public string? Title { get; init; }
    }

    private sealed class BlockRow
    {
        public required string TaskId { get; init; }
        public string? Status { get; init; }
        public string? Title { get; init; }
    }
}
