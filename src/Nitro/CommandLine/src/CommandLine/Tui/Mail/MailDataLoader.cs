using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// Loads workspace thread summaries from the mail store.
/// </summary>
internal sealed class MailDataLoader(IMailStore store)
{
    /// <summary>
    /// Loads every workspace thread summary, newest activity first, narrowed to threads
    /// <paramref name="agent"/> sent or received mail in, or across every agent when null.
    /// </summary>
    public Task<IReadOnlyList<MailThreadSummary>> LoadWorkspaceThreadsAsync(
        string? agent,
        CancellationToken cancellationToken)
        => store.QueryWorkspaceThreadsAsync(agent, cancellationToken);
}
