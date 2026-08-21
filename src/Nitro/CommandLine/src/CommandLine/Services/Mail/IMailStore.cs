namespace ChilliCream.Nitro.CommandLine.Services.Mail;

/// <summary>
/// Backend-agnostic mail store used by every mail command. No member exposes
/// ADO.NET or SQLite types, so the backend can change without touching a
/// command and the interface can be mocked for the TUI.
/// </summary>
internal interface IMailStore
{
    /// <summary>
    /// Returns the nearest workspace directory at or above the current
    /// directory, or null when no workspace exists.
    /// </summary>
    string? FindWorkspaceDirectory();

    /// <summary>
    /// Creates a workspace database in the given directory and applies the
    /// schema.
    /// </summary>
    Task InitializeWorkspaceAsync(
        string workspaceDirectory,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sends a message: normalizes the sender and recipients, auto-registers
    /// the sender (upsert, bumps last_seen_at), dedupes recipients (to wins
    /// over cc, first occurrence order preserved), and writes the message
    /// and its recipient rows in one transaction. Starts a new thread:
    /// <c>ThreadId</c> equals the new message's id. A recipient that has
    /// never registered gets an implicit agent row rather than failing the
    /// send; the returned message's <see cref="MailMessage.Unregistered"/>
    /// lists every recipient, implicit before or because of this call, that
    /// has still never registered. Throws <see cref="ExitException"/> when
    /// the subject is empty, a recipient name is invalid, or no recipient
    /// remains after dedupe.
    /// </summary>
    Task<MailMessage> SendMessageAsync(
        MailMessageCreation creation,
        CancellationToken cancellationToken);

    /// <summary>
    /// Replies to a message: the acting agent must be the sender or a
    /// recipient of the message being replied to. The reply's recipients
    /// are the original message's sender plus its to and cc recipients,
    /// minus the acting agent; its subject is inherited from the thread
    /// root and its thread_id copied from the original message. Throws
    /// <see cref="ExitException"/> when the original message does not
    /// exist, the actor is not authorized to reply, or the computed
    /// recipient set is empty.
    /// </summary>
    Task<MailMessage> ReplyMessageAsync(
        string inReplyToId,
        string sender,
        string body,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns the message with the given ID, with its recipients embedded,
    /// or null.
    /// </summary>
    Task<MailMessage?> GetMessageAsync(
        string id,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns the message with the given ID or throws
    /// <see cref="ExitException"/> when it does not exist.
    /// </summary>
    Task<MailMessage> GetRequiredMessageAsync(
        string id,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns every message in a thread, with recipients embedded, ordered
    /// by created_at then id, oldest first.
    /// </summary>
    Task<IReadOnlyList<MailMessage>> GetThreadMessagesAsync(
        string threadId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns the messages addressed to <see cref="MailInboxFilter.Actor"/>
    /// matching the given filter, with recipients embedded, ordered by
    /// created_at then id, newest first.
    /// </summary>
    Task<IReadOnlyList<MailMessage>> QueryInboxAsync(
        MailInboxFilter filter,
        CancellationToken cancellationToken);

    /// <summary>
    /// Marks every given message read for the acting agent. All ids are
    /// validated as addressed to the actor before any write happens: either
    /// every message is marked or none is. Throws
    /// <see cref="ExitException"/> when any message is not addressed to the
    /// actor.
    /// </summary>
    Task MarkReadAsync(
        IReadOnlyList<string> messageIds,
        string actor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Marks every given message unread for the acting agent. All ids are
    /// validated as addressed to the actor before any write happens: either
    /// every message is marked or none is. Throws
    /// <see cref="ExitException"/> when any message is not addressed to the
    /// actor.
    /// </summary>
    Task MarkUnreadAsync(
        IReadOnlyList<string> messageIds,
        string actor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Archives every given message for the acting agent. All ids are
    /// validated as addressed to the actor before any write happens: either
    /// every message is archived or none is. Throws
    /// <see cref="ExitException"/> when any message is not addressed to the
    /// actor.
    /// </summary>
    Task ArchiveAsync(
        IReadOnlyList<string> messageIds,
        string actor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns every thread the given agent sent or received a message in,
    /// as sent or unarchived-received counts as participation, ordered by
    /// the thread's last message, newest first.
    /// </summary>
    Task<IReadOnlyList<MailThreadSummary>> QueryThreadsAsync(
        string actor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns the messages the given agent sent or received whose subject
    /// or body contains the given text, matched case-insensitively over
    /// ASCII, ordered by created_at then id, newest first.
    /// </summary>
    Task<IReadOnlyList<MailMessage>> SearchAsync(
        string actor,
        string text,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns how many messages addressed to the given agent are unread
    /// and not archived.
    /// </summary>
    Task<int> CountUnreadAsync(
        string actor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns the messages the given agent sent, regardless of recipients,
    /// with recipients embedded, ordered by created_at then id, newest
    /// first, optionally capped at <paramref name="limit"/>.
    /// </summary>
    Task<IReadOnlyList<MailMessage>> QuerySentAsync(
        string sender,
        int? limit,
        CancellationToken cancellationToken);
}
