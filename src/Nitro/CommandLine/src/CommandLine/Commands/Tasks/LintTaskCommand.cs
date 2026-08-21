using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class LintTaskCommand : Command
{
    public LintTaskCommand() : base("lint")
    {
        Description = "Report task quality findings.";

        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent tasks lint");

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

        var findings = await CollectFindingsAsync(store, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ListResult<TaskLintFinding>(findings));
            return ExitCodes.Success;
        }

        if (findings.Count == 0)
        {
            console.WriteLine("No lint findings.");
            return ExitCodes.Success;
        }

        foreach (var finding in findings)
        {
            console.WriteLine($"{finding.TaskId}  {finding.Rule}: {finding.Message}");
        }

        return ExitCodes.Success;
    }

    private static async Task<List<TaskLintFinding>> CollectFindingsAsync(
        ITaskStore store,
        CancellationToken cancellationToken)
    {
        var findings = new List<TaskLintFinding>();

        var tasks = await store.QueryTasksAsync(
            new TaskFilter { IncludeAll = true, IncludeArchived = true, ExcludeTombstones = true },
            cancellationToken);

        foreach (var task in tasks)
        {
            if (task.Status == TaskStates.Open && string.IsNullOrWhiteSpace(task.Description))
            {
                findings.Add(new TaskLintFinding(
                    task.Id, "empty-description", "Open task has an empty description."));
            }

            if (task.Status == TaskStates.Deferred && task.DeferUntil is null)
            {
                findings.Add(new TaskLintFinding(
                    task.Id, "deferred-without-defer-until", "Deferred task has no defer_until."));
            }

            if (task.Status == TaskStates.InProgress && string.IsNullOrEmpty(task.Assignee))
            {
                findings.Add(new TaskLintFinding(
                    task.Id, "in-progress-without-assignee", "In-progress task has no assignee."));
            }
        }

        var epicStatuses = await store.GetEpicStatusesAsync(cancellationToken);

        foreach (var epic in epicStatuses)
        {
            if (epic.Total == 0)
            {
                findings.Add(new TaskLintFinding(
                    epic.Id, "epic-no-children", "Epic has zero children."));
            }
            else if (epic.Status == TaskStates.Closed && epic.Closed < epic.Total)
            {
                findings.Add(new TaskLintFinding(
                    epic.Id, "closed-epic-open-children", "Closed epic has open children."));
            }
        }

        findings.Sort((left, right) =>
        {
            var byTaskId = string.CompareOrdinal(left.TaskId, right.TaskId);
            return byTaskId != 0 ? byTaskId : string.CompareOrdinal(left.Rule, right.Rule);
        });

        return findings;
    }
}
