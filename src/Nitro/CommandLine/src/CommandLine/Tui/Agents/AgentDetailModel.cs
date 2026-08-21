using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// The loaded state behind the agent detail view: the selected agent's
/// identity, the tasks assigned to it, and the mail it has sent, each
/// re-fetched independently on every <see cref="LoadAsync"/> call.
/// </summary>
internal sealed class AgentDetailModel
{
    /// <summary>
    /// The cap on how many sent messages are loaded, matching the ticket's
    /// "sent mail, limit ~20" direction.
    /// </summary>
    private const int SentMailLimit = 20;

    private readonly IAgentRegistry _registry;
    private readonly ITaskStore _taskStore;
    private readonly IMailStore _mailStore;

    public AgentDetailModel(IAgentRegistry registry, ITaskStore taskStore, IMailStore mailStore)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _taskStore = taskStore ?? throw new ArgumentNullException(nameof(taskStore));
        _mailStore = mailStore ?? throw new ArgumentNullException(nameof(mailStore));
    }

    /// <summary>
    /// The name last passed to <see cref="LoadAsync"/>, even when it does
    /// not resolve to a registered agent.
    /// </summary>
    public string? CurrentAgentName { get; private set; }

    /// <summary>
    /// The currently loaded agent's identity, or null when the last load
    /// target does not exist.
    /// </summary>
    public AgentRecord? Agent { get; private set; }

    /// <summary>
    /// The tasks assigned to the loaded agent, in-progress tasks first, then
    /// open, then any other non-closed, non-tombstone status, each group in
    /// the store's own order.
    /// </summary>
    public IReadOnlyList<TaskItem> Tasks { get; private set; } = [];

    /// <summary>
    /// The loaded agent's most recent sent messages, newest first, capped at
    /// <see cref="SentMailLimit"/>.
    /// </summary>
    public IReadOnlyList<MailMessage> SentMail { get; private set; } = [];

    /// <summary>
    /// Loads the agent with the given name, its assigned tasks, and its sent
    /// mail, replacing whichever agent was previously loaded.
    /// </summary>
    public async Task LoadAsync(string name, CancellationToken cancellationToken)
    {
        CurrentAgentName = name;

        Agent = await _registry.GetAsync(name, cancellationToken).ConfigureAwait(false);

        var tasks = await _taskStore.QueryTasksAsync(
            new TaskFilter { Assignee = name },
            cancellationToken).ConfigureAwait(false);
        Tasks = OrderTasks(tasks);

        SentMail = await _mailStore.QuerySentAsync(name, SentMailLimit, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Stably reorders tasks so in-progress tasks come first, then open,
    /// then everything else, preserving the store's own order within each
    /// group.
    /// </summary>
    private static IReadOnlyList<TaskItem> OrderTasks(IReadOnlyList<TaskItem> tasks)
        => tasks.OrderBy(StatusRank).ToList();

    private static int StatusRank(TaskItem task) => task.Status switch
    {
        TaskStates.InProgress => 0,
        TaskStates.Open => 1,
        _ => 2
    };
}
