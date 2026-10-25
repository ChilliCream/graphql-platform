namespace ChilliCream.Nitro.CommandLine.Services.Mail;

/// <summary>
/// Stores, queries, and updates workspace mail and per-recipient read and archive state.
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
    /// Starts a thread with normalized addresses and recipients deduplicated in first
    /// occurrence order (To wins over Cc). The sender must exist and not be deleted.
    /// Stores the message and recipients atomically with wake generations when
    /// <see cref="MailMessageCreation.WakePolicy"/> requests them; invalid addresses,
    /// a trimmed subject outside 1 to 500 characters, no recipients, or an unusable
    /// sender or recipient cause an <see cref="ExitException"/>.
    /// </summary>
    Task<MailMessage> SendMessageAsync(
        MailMessageCreation creation,
        CancellationToken cancellationToken);

    /// <summary>
    /// Replies in the original thread with its root subject, addressing the original
    /// sender and recipients as To recipients except the replying actor, and applying
    /// <paramref name="wakePolicy"/>. The actor must exist and not be deleted; among
    /// several remaining recipients an unknown or deleted one is dropped and reported
    /// in <see cref="MailMessage.Skipped"/>, but a lone remaining recipient must be
    /// usable. Throws <see cref="ExitException"/> if the message is missing, the actor
    /// is not a participant or is unusable, or no usable recipient remains.
    /// </summary>
    Task<MailMessage> ReplyMessageAsync(
        string inReplyToId,
        string sender,
        string body,
        MailWakePolicy wakePolicy,
        CancellationToken cancellationToken);

    /// <summary>
    /// Replies without advancing any recipient's wake generation.
    /// </summary>
    Task<MailMessage> ReplyMessageAsync(
        string inReplyToId,
        string sender,
        string body,
        CancellationToken cancellationToken)
        => ReplyMessageAsync(inReplyToId, sender, body, MailWakePolicy.Skip, cancellationToken);

    /// <summary>
    /// Moves the source agent's unread, unarchived recipient rows to the target agent,
    /// leaving read or archived rows and every message's sender untouched. Recipient
    /// conflicts preserve the target agent's recipient state.
    /// </summary>
    Task<MailTransferResult> TransferParticipationAsync(
        string from,
        string to,
        CancellationToken cancellationToken)
        => Task.FromException<MailTransferResult>(new NotSupportedException());

    /// <summary>
    /// Returns the message and its recipients, or null when the id does not exist.
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
    /// Returns matching workspace messages with their recipients, ordered by creation
    /// time and id, newest first. No acting-actor scope is applied.
    /// </summary>
    Task<IReadOnlyList<MailMessage>> QueryWorkspaceMessagesAsync(
        MailWorkspaceFilter filter,
        CancellationToken cancellationToken);

    /// <summary>
    /// Marks every supplied message read for the actor atomically.
    /// Throws <see cref="ExitException"/> without changing any message if an id is missing
    /// or is not addressed to the actor.
    /// </summary>
    Task MarkReadAsync(
        IReadOnlyList<string> messageIds,
        string actor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Marks every supplied message unread for the actor atomically.
    /// Throws <see cref="ExitException"/> without changing any message if an id is missing
    /// or is not addressed to the actor.
    /// </summary>
    Task MarkUnreadAsync(
        IReadOnlyList<string> messageIds,
        string actor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Marks every supplied message archived for the actor atomically.
    /// Throws <see cref="ExitException"/> without changing any message if an id is missing
    /// or is not addressed to the actor.
    /// </summary>
    Task ArchiveAsync(
        IReadOnlyList<string> messageIds,
        string actor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns threads in which the actor sent or received mail, including archived mail,
    /// ordered by last-message time and thread id, newest first.
    /// Unread and archived counts are scoped to the actor.
    /// </summary>
    Task<IReadOnlyList<MailThreadSummary>> QueryThreadsAsync(
        string actor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns threads with mail addressed to the actor, ordered by last-message time
    /// and thread id, newest first, with actor-scoped counts.
    /// When <paramref name="includeArchived"/> is false, a thread must contain
    /// at least one unarchived message addressed to the actor.
    /// </summary>
    Task<IReadOnlyList<MailThreadSummary>> QueryInboxThreadsAsync(
        string actor,
        bool includeArchived,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns threads containing mail sent by the actor, ordered by last-message time
    /// and thread id, newest first. Unread and archived counts are scoped to the actor.
    /// </summary>
    Task<IReadOnlyList<MailThreadSummary>> QuerySentThreadsAsync(
        string actor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns workspace threads ordered by last-message time and thread id, newest first,
    /// with null unread and archived counts. A nonempty <paramref name="agent"/> restricts
    /// results to that actor's sent or received threads.
    /// </summary>
    Task<IReadOnlyList<MailThreadSummary>> QueryWorkspaceThreadsAsync(
        string? agent,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns one row per thread where the agent sent a message or was a to or cc recipient
    /// of one, ranked by the agent's own newest such message in the thread then thread id
    /// descending, and capped by a non-null <paramref name="limit"/>. Unread and archived
    /// counts are scoped to the agent.
    /// </summary>
    Task<IReadOnlyList<MailThreadSummary>> QueryParticipationThreadsAsync(
        string agent,
        int? limit,
        CancellationToken cancellationToken)
        => Task.FromException<IReadOnlyList<MailThreadSummary>>(new NotSupportedException());

    /// <summary>
    /// Returns the actor's sent or received messages whose subject, body, or sender
    /// contains the text, matching ASCII case-insensitively.
    /// Results include recipients and are ordered by creation time and id, newest first.
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
    /// Returns the sender's messages with their recipients, ordered by creation time
    /// and id, newest first. A null <paramref name="limit"/> leaves results uncapped;
    /// read and archived state belong only to message recipients.
    /// </summary>
    Task<IReadOnlyList<MailMessage>> QuerySentAsync(
        string sender,
        int? limit,
        CancellationToken cancellationToken);
}
