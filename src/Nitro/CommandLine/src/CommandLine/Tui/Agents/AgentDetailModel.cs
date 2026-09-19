using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// The selected participant, its bound identity, assigned tasks, and recent sent
/// mail. An unbound session has no tasks or sent mail.
/// </summary>
internal sealed class AgentDetailModel
{
    /// <summary>
    /// The cap on how many sent messages are loaded.
    /// </summary>
    private const int SentMailLimit = 20;

    private readonly ITaskStore _taskStore;
    private readonly IMailStore _mailStore;

    public AgentDetailModel(ITaskStore taskStore, IMailStore mailStore)
    {
        _taskStore = taskStore ?? throw new ArgumentNullException(nameof(taskStore));
        _mailStore = mailStore ?? throw new ArgumentNullException(nameof(mailStore));
    }

    /// <summary>
    /// The key of the participant last passed to <see cref="LoadAsync"/>, or
    /// null before the first load or after <see cref="Clear"/>.
    /// </summary>
    public AgentSessionKey? CurrentKey { get; private set; }

    /// <summary>
    /// The currently loaded participant, or null before the first load or
    /// after <see cref="Clear"/>.
    /// </summary>
    public AgentSessionParticipant? Participant { get; private set; }

    /// <summary>
    /// Non-terminal tasks assigned to the bound actor, with in-progress tasks first,
    /// then open tasks, then other states in store order. Empty for an unbound session.
    /// </summary>
    public IReadOnlyList<TaskItem> Tasks { get; private set; } = [];

    /// <summary>
    /// The loaded participant's bound actor's most recent sent messages,
    /// empty for an unbound session, newest first, capped at
    /// <see cref="SentMailLimit"/>.
    /// </summary>
    public IReadOnlyList<MailMessage> SentMail { get; private set; } = [];

    /// <summary>
    /// Loads <paramref name="participant"/>'s session diagnostics, and, when
    /// its session is bound to an actor, that actor's assigned tasks and
    /// sent mail, replacing whichever participant was previously loaded.
    /// </summary>
    public async Task LoadAsync(AgentSessionParticipant participant, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(participant);

        CurrentKey = AgentSessionKey.From(participant.Session);
        Participant = participant;

        if (participant.Session.AgentName is not { } actor)
        {
            Tasks = [];
            SentMail = [];
            return;
        }

        var tasks = await _taskStore.QueryTasksAsync(
            new TaskFilter { Assignee = actor },
            cancellationToken).ConfigureAwait(false);
        Tasks = OrderTasks(tasks);

        SentMail = await _mailStore.QuerySentAsync(actor, SentMailLimit, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Resets the model to its unloaded state: no current key, participant, tasks, or sent mail.
    /// </summary>
    public void Clear()
    {
        CurrentKey = null;
        Participant = null;
        Tasks = [];
        SentMail = [];
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
