using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// A <see cref="MailReplyForm"/> submission snapshot: the message it replies
/// to, the acting agent, and the reply body, ready for
/// <see cref="IMailStore.ReplyMessageAsync(string, string, string, MailWakePolicy, CancellationToken)"/>
/// with <see cref="MailWakePolicy.Enqueue"/>, matching the CLI's own reply
/// command.
/// </summary>
internal sealed record MailReplyRequest(string InReplyToId, string Actor, string Body);
