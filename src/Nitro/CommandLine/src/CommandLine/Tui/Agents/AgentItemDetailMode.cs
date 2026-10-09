using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Details;
using ChilliCream.Nitro.CommandLine.Tui.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Memory;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// A read-only nested view drilled into from a popover row or its show-more list: a mail
/// thread, a ticket, or a memory entry, loaded once and rendered with the owning tab's own
/// detail renderer. Scrolls one line at a time and exposes the drilled item's id for the
/// popover's copy-id gesture.
/// </summary>
internal sealed class AgentItemDetailMode
{
    private readonly Func<int, int, IRenderable> _render;
    private readonly Action _scrollDown;
    private readonly Action _scrollUp;

    private AgentItemDetailMode(string copyId, Func<int, int, IRenderable> render, Action scrollDown, Action scrollUp)
    {
        CopyId = copyId;
        _render = render;
        _scrollDown = scrollDown;
        _scrollUp = scrollUp;
    }

    /// <summary>
    /// The id the popover's copy-id gesture copies: the thread, task, or memory id.
    /// </summary>
    public string CopyId { get; }

    /// <summary>
    /// Loads the mail thread with the given id and renders it with the Mail tab's thread
    /// renderer, oldest message first. Never marks any message read.
    /// </summary>
    public static AgentItemDetailMode ForThread(IMailStore mailStore, string threadId)
    {
        ArgumentNullException.ThrowIfNull(mailStore);
        ArgumentException.ThrowIfNullOrEmpty(threadId);

        var messages = mailStore.GetThreadMessagesAsync(threadId, CancellationToken.None).GetAwaiter().GetResult();
        var view = new MailDetailView();

        return new AgentItemDetailMode(
            threadId,
            (width, height) => view.RenderThread(messages, width, height, focused: true),
            view.ScrollDown,
            view.ScrollUp);
    }

    /// <summary>
    /// Loads the task with the given id and renders it with the Tasks tab's detail
    /// renderer: body, comments, and sidebar.
    /// </summary>
    public static AgentItemDetailMode ForTask(ITaskStore taskStore, string taskId)
    {
        ArgumentNullException.ThrowIfNull(taskStore);
        ArgumentException.ThrowIfNullOrEmpty(taskId);

        var model = new TaskDetailModel(taskStore);
        model.LoadAsync(taskId, CancellationToken.None).GetAwaiter().GetResult();
        var view = new TaskDetailView(model);

        return new AgentItemDetailMode(
            taskId,
            (width, height) => view.Render(width, height, focused: true),
            view.ScrollDown,
            view.ScrollUp);
    }

    /// <summary>
    /// Loads the curated memory or journal entry with the given id and kind and renders it
    /// with the Memory tab's detail renderer.
    /// </summary>
    public static AgentItemDetailMode ForMemory(IMemoryStore memoryStore, MemoryParticipationKind kind, string id)
    {
        ArgumentNullException.ThrowIfNull(memoryStore);
        ArgumentException.ThrowIfNullOrEmpty(id);

        var view = new MemoryDetailView();

        if (kind == MemoryParticipationKind.Curated)
        {
            var record = memoryStore.GetRequiredAsync(id, CancellationToken.None).GetAwaiter().GetResult();

            return new AgentItemDetailMode(
                id,
                (width, height) => view.RenderCurated(record, width, height, focused: true),
                view.ScrollDown,
                view.ScrollUp);
        }

        var entry = memoryStore.GetRequiredJournalEntryAsync(id, CancellationToken.None).GetAwaiter().GetResult();

        return new AgentItemDetailMode(
            id,
            (width, height) => view.RenderJournal(entry, width, height, focused: true),
            view.ScrollDown,
            view.ScrollUp);
    }

    /// <summary>
    /// Renders the item at the given size.
    /// </summary>
    public IRenderable Render(int width, int height) => _render(width, height);

    /// <summary>
    /// Scrolls the item's body down one line.
    /// </summary>
    public void ScrollDown() => _scrollDown();

    /// <summary>
    /// Scrolls the item's body up one line.
    /// </summary>
    public void ScrollUp() => _scrollUp();
}
