using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// The mail board's live state: the acting agent's inbox for the current
/// filter, the selected message, which pane has focus, and the detail
/// pane's view mode.
/// </summary>
internal sealed class MailState(string actor, MailDataLoader loader)
{
    /// <summary>
    /// The acting agent whose inbox this state loads.
    /// </summary>
    public string Actor { get; } = actor;

    /// <summary>
    /// The filter currently applied to <see cref="Messages"/>.
    /// </summary>
    public MailListFilter Filter { get; private set; } = MailListFilter.Inbox;

    /// <summary>
    /// The messages currently loaded for <see cref="Filter"/>, newest first.
    /// </summary>
    public IReadOnlyList<MailMessage> Messages { get; private set; } = [];

    /// <summary>
    /// The index of the selected row within <see cref="Messages"/>.
    /// </summary>
    public int SelectedRow { get; set; }

    /// <summary>
    /// Which pane currently holds focus.
    /// </summary>
    public MailFocus Focus { get; set; } = MailFocus.List;

    /// <summary>
    /// What the detail pane currently renders.
    /// </summary>
    public MailViewMode ViewMode { get; private set; } = MailViewMode.Message;

    /// <summary>
    /// The selected message's thread, loaded when <see cref="ViewMode"/> is
    /// <see cref="MailViewMode.Thread"/>.
    /// </summary>
    public IReadOnlyList<MailMessage> ThreadMessages { get; private set; } = [];

    /// <summary>
    /// The message at <see cref="SelectedRow"/>, or null when
    /// <see cref="Messages"/> is empty or the row is out of range.
    /// </summary>
    public MailMessage? SelectedMessage
        => SelectedRow >= 0 && SelectedRow < Messages.Count ? Messages[SelectedRow] : null;

    /// <summary>
    /// Reloads <see cref="Messages"/> for the current filter. The selected
    /// message stays selected when it is still present in the reloaded
    /// list; otherwise the selected row is clamped to the new list's
    /// bounds. Also reloads <see cref="ThreadMessages"/> when
    /// <see cref="ViewMode"/> is <see cref="MailViewMode.Thread"/>.
    /// </summary>
    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var selectedId = SelectedMessage?.Id;

        Messages = await loader.LoadInboxAsync(Actor, Filter, cancellationToken).ConfigureAwait(false);

        var preservedIndex = selectedId is null ? -1 : IndexOf(Messages, selectedId);
        SelectedRow = preservedIndex >= 0
            ? preservedIndex
            : Math.Clamp(SelectedRow, 0, Math.Max(0, Messages.Count - 1));

        if (ViewMode == MailViewMode.Thread)
        {
            await ReloadThreadAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Changes <see cref="Filter"/> by <paramref name="delta"/> positions,
    /// cycling through the three <see cref="MailListFilter"/> values, and
    /// reloads <see cref="Messages"/>.
    /// </summary>
    public async Task CycleFilterAsync(int delta, CancellationToken cancellationToken)
    {
        var values = Enum.GetValues<MailListFilter>();
        var index = ((int)Filter + delta) % values.Length;

        if (index < 0)
        {
            index += values.Length;
        }

        Filter = values[index];
        await RefreshAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Switches the detail pane to <see cref="MailViewMode.Thread"/>,
    /// loading the selected message's thread. Has no effect, and returns
    /// false, when no message is selected.
    /// </summary>
    public async Task<bool> ShowThreadAsync(CancellationToken cancellationToken)
    {
        if (SelectedMessage is not { } message)
        {
            return false;
        }

        ViewMode = MailViewMode.Thread;
        ThreadMessages = await loader.LoadThreadAsync(message.ThreadId, cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Switches the detail pane back to <see cref="MailViewMode.Message"/>.
    /// </summary>
    public void ShowMessage()
    {
        ViewMode = MailViewMode.Message;
        ThreadMessages = [];
    }

    private async Task ReloadThreadAsync(CancellationToken cancellationToken)
    {
        ThreadMessages = SelectedMessage is { } message
            ? await loader.LoadThreadAsync(message.ThreadId, cancellationToken).ConfigureAwait(false)
            : [];
    }

    private static int IndexOf(IReadOnlyList<MailMessage> messages, string id)
    {
        for (var i = 0; i < messages.Count; i++)
        {
            if (messages[i].Id == id)
            {
                return i;
            }
        }

        return -1;
    }
}
