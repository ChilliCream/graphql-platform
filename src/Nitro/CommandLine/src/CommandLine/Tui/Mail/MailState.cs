using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// The loaded workspace thread rows, agent filter, search filter, and selection for the
/// Mail tab.
/// </summary>
internal sealed class MailState(MailDataLoader loader)
{
    private IReadOnlyList<MailThreadSummary> _allThreads = [];

    /// <summary>
    /// Every workspace thread matching <see cref="AgentFilter"/> and <see cref="SearchText"/>,
    /// as loaded by the last <see cref="RefreshAsync"/> or <see cref="SelectAgentFilterAsync"/>
    /// call: last-message time descending, then thread id descending.
    /// </summary>
    public IReadOnlyList<MailThreadSummary> Threads { get; private set; } = [];

    /// <summary>
    /// The total thread count for the current <see cref="AgentFilter"/>, independent of
    /// <see cref="SearchText"/>.
    /// </summary>
    public int TotalCount => _allThreads.Count;

    /// <summary>
    /// The index of the selected row within <see cref="Threads"/>.
    /// </summary>
    public int SelectedRow { get; set; }

    /// <summary>
    /// The current subject/sender/recipient search filter; empty shows every thread.
    /// </summary>
    public string SearchText { get; private set; } = string.Empty;

    /// <summary>
    /// The agent whose sent or received mail narrows <see cref="Threads"/>, or null for
    /// every agent.
    /// </summary>
    public string? AgentFilter { get; private set; }

    /// <summary>
    /// The thread at <see cref="SelectedRow"/>, or null when there is no such row.
    /// </summary>
    public MailThreadSummary? SelectedThread
        => SelectedRow >= 0 && SelectedRow < Threads.Count ? Threads[SelectedRow] : null;

    /// <summary>
    /// Reloads every workspace thread for <see cref="AgentFilter"/> from the mail store,
    /// re-applies the search filter, and keeps the previously selected thread selected by
    /// id when it is still present; otherwise clamps the selection to the new list's bounds.
    /// </summary>
    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var selectedId = SelectedThread?.ThreadId;

        _allThreads = await loader.LoadWorkspaceThreadsAsync(AgentFilter, cancellationToken).ConfigureAwait(false);
        ApplySearchFilter();

        var preservedIndex = selectedId is null ? -1 : IndexOf(Threads, selectedId);

        SelectedRow = preservedIndex >= 0
            ? preservedIndex
            : Math.Clamp(SelectedRow, 0, Math.Max(0, Threads.Count - 1));
    }

    /// <summary>
    /// Sets <see cref="AgentFilter"/>, reloads from the mail store, and selects the first row.
    /// </summary>
    public async Task SelectAgentFilterAsync(string? agent, CancellationToken cancellationToken)
    {
        AgentFilter = agent;
        _allThreads = await loader.LoadWorkspaceThreadsAsync(AgentFilter, cancellationToken).ConfigureAwait(false);
        ApplySearchFilter();
        SelectedRow = 0;
    }

    /// <summary>
    /// Applies a new search filter over subject, last sender, and last recipients, and
    /// selects the first matching row.
    /// </summary>
    public void ApplySearch(string text)
    {
        SearchText = text.Trim();
        ApplySearchFilter();
        SelectedRow = 0;
    }

    private void ApplySearchFilter()
    {
        Threads = SearchText.Length == 0
            ? _allThreads
            : _allThreads.Where(Matches).ToList();
    }

    private bool Matches(MailThreadSummary thread)
        => thread.Subject.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || thread.LastSender.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || thread.LastRecipients.Any(name => name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

    private static int IndexOf(IReadOnlyList<MailThreadSummary> threads, string threadId)
    {
        for (var i = 0; i < threads.Count; i++)
        {
            if (threads[i].ThreadId == threadId)
            {
                return i;
            }
        }

        return -1;
    }
}
